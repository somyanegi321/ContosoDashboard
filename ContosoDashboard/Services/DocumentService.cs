using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace ContosoDashboard.Services;

public sealed record DocumentSummary(int DocumentId, string Title, string Category, DateTime UploadedAt, long FileSize, string FileType, string? ProjectName, string UploaderName, string? Description, string? Tags);
public sealed record DocumentQuery(string? Search = null, string? Category = null, int? ProjectId = null, DateTime? From = null, DateTime? To = null, string Sort = "date", int Page = 1, int PageSize = 25);
public sealed record DocumentPage(IReadOnlyList<DocumentSummary> Items, int TotalCount, int Page, int PageSize);
public sealed record DocumentMetadataChanges(string Title, string? Description, string Category, IReadOnlyCollection<string> Tags);
public sealed record DocumentShareTarget(int? UserId, string? Department);
public sealed record AdminDocumentReport(IReadOnlyList<(string Type, int Count)> FileTypes, IReadOnlyList<(string User, int Count)> Uploaders, IReadOnlyList<(string Action, int Count)> AccessPatterns);

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IMalwareScanner _scanner;
    private readonly DocumentAuthorizationService _authorization;
    private readonly INotificationService _notifications;
    private readonly DocumentFeatureOptions _options;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(ApplicationDbContext context, IFileStorageService storage, IMalwareScanner scanner, DocumentAuthorizationService authorization, INotificationService notifications, IOptions<DocumentFeatureOptions> options, ILogger<DocumentService> logger)
    {
        _context = context; _storage = storage; _scanner = scanner; _authorization = authorization; _notifications = notifications; _options = options.Value; _logger = logger;
    }

    public async Task<DocumentUploadResult> UploadAsync(int requestingUserId, DocumentUploadMetadata metadata, Stream content, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateMetadata(metadata);
        }
        catch (DocumentUploadException exception)
        {
            await RecordActivityAsync(null, requestingUserId, "Upload", "Rejected", exception.Message, cancellationToken);
            throw;
        }
        if (metadata.ProjectId.HasValue && !await _authorization.CanViewProjectAsync(metadata.ProjectId.Value, requestingUserId, cancellationToken))
        {
            await RecordActivityAsync(null, requestingUserId, "Upload", "Denied", "Project access denied", cancellationToken);
            throw new DocumentUploadException(DocumentUploadErrorCode.Unauthorized, "You cannot upload to this project.");
        }
        if (metadata.TaskId.HasValue)
        {
            var task = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.TaskId == metadata.TaskId, cancellationToken);
            if (task == null || task.ProjectId != metadata.ProjectId) throw new DocumentUploadException(DocumentUploadErrorCode.InvalidRequest, "The task and project must match.");
        }
        if (metadata.FileSize > _options.MaximumFileSize)
            throw new DocumentUploadException(DocumentUploadErrorCode.FileTooLarge, "The file exceeds the configured size limit.");

        var scanStream = new MemoryStream();
        await content.CopyToAsync(scanStream, cancellationToken);
        scanStream.Position = 0;
        var scan = await _scanner.ScanAsync(scanStream, metadata.OriginalFileName, metadata.ContentType, cancellationToken);
        if (scan == MalwareScanResult.Infected)
        {
            await RecordActivityAsync(null, requestingUserId, "Upload", "Rejected", "Malware detected", cancellationToken);
            throw new DocumentUploadException(DocumentUploadErrorCode.MalwareDetected, "The file failed the security scan.");
        }
        if (scan != MalwareScanResult.Clean)
        {
            await RecordActivityAsync(null, requestingUserId, "Upload", "Rejected", "Security scan unavailable", cancellationToken);
            throw new DocumentUploadException(DocumentUploadErrorCode.ScanUnavailable, "The file could not be security scanned.");
        }

        var relativePath = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{Path.GetExtension(metadata.OriginalFileName).ToLowerInvariant()}";
        var stored = false;
        try
        {
            scanStream.Position = 0;
            await _storage.UploadAsync(scanStream, relativePath, metadata.ContentType, cancellationToken);
            stored = true;
            var document = new Document
            {
                Title = metadata.Title, Description = metadata.Description, Category = metadata.Category,
                Tags = string.Join(", ", metadata.Tags), OriginalFileName = metadata.OriginalFileName,
                FilePath = relativePath, FileSize = metadata.FileSize, FileType = metadata.ContentType,
                UploadedAt = DateTime.UtcNow, UploadedByUserId = requestingUserId, ProjectId = metadata.ProjectId, TaskId = metadata.TaskId
            };
            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);
            await RecordActivityAsync(document.DocumentId, requestingUserId, "Upload", "Succeeded", null, cancellationToken);
            if (document.ProjectId.HasValue)
            {
                var recipients = await _context.ProjectMembers.Where(m => m.ProjectId == document.ProjectId && m.UserId != requestingUserId).Select(m => m.UserId).ToListAsync(cancellationToken);
                await _notifications.NotifyProjectDocumentAddedAsync(document, recipients, "A team member");
            }
            return new DocumentUploadResult(document.DocumentId, document.Title);
        }
        catch (DbUpdateException exception)
        {
            if (stored) await _storage.DeleteAsync(relativePath, cancellationToken);
            _logger.LogError(exception, "Document metadata persistence failed for user {UserId}.", requestingUserId);
            throw new DocumentUploadException(DocumentUploadErrorCode.StorageFailure, "The document could not be saved.", exception);
        }
        finally { scanStream.Dispose(); }
    }

    public async Task<DocumentPage> GetMyDocumentsAsync(int userId, DocumentQuery query, CancellationToken cancellationToken = default)
    {
        var documents = AccessibleQuery(userId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            documents = documents.Where(d => d.Title.Contains(term) || (d.Description != null && d.Description.Contains(term)) || (d.Tags != null && d.Tags.Contains(term)) || d.UploadedByUser.DisplayName.Contains(term) || (d.Project != null && d.Project.Name.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(query.Category)) documents = documents.Where(d => d.Category == query.Category);
        if (query.ProjectId.HasValue) documents = documents.Where(d => d.ProjectId == query.ProjectId);
        if (query.From.HasValue) documents = documents.Where(d => d.UploadedAt >= query.From);
        if (query.To.HasValue) documents = documents.Where(d => d.UploadedAt < query.To.Value.Date.AddDays(1));
        documents = query.Sort.ToLowerInvariant() switch
        {
            "title" => documents.OrderBy(d => d.Title),
            "category" => documents.OrderBy(d => d.Category).ThenByDescending(d => d.UploadedAt),
            "size" => documents.OrderByDescending(d => d.FileSize),
            _ => documents.OrderByDescending(d => d.UploadedAt)
        };
        var total = await documents.CountAsync(cancellationToken);
        var page = Math.Max(1, query.Page);
        var items = await documents.Skip((page - 1) * query.PageSize).Take(query.PageSize).Select(ToSummary()).ToListAsync(cancellationToken);
        return new DocumentPage(items, total, page, query.PageSize);
    }

    public Task<DocumentPage> SearchAsync(int userId, DocumentQuery query, CancellationToken cancellationToken = default) => GetMyDocumentsAsync(userId, query, cancellationToken);
    public async Task<List<DocumentSummary>> GetProjectDocumentsAsync(int userId, int projectId, CancellationToken cancellationToken = default) => (await GetMyDocumentsAsync(userId, new DocumentQuery(ProjectId: projectId, PageSize: 500), cancellationToken)).Items.ToList();
    public async Task<List<DocumentSummary>> GetTaskDocumentsAsync(int userId, int taskId, CancellationToken cancellationToken = default) => (await GetMyDocumentsAsync(userId, new DocumentQuery(PageSize: 500), cancellationToken)).Items.Where(d => _context.Documents.Any(x => x.DocumentId == d.DocumentId && x.TaskId == taskId)).ToList();
    public async Task<bool> AttachToTaskAsync(int userId, int documentId, int taskId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        var task = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.TaskId == taskId, cancellationToken);
        if (document is null || task is null || !await _authorization.CanManageAsync(document, userId, cancellationToken)) return false;
        if (document.ProjectId != task.ProjectId) throw new DocumentUploadException(DocumentUploadErrorCode.InvalidRequest, "The document and task must belong to the same project.");
        document.TaskId = taskId;
        await _context.SaveChangesAsync(cancellationToken);
        await RecordActivityAsync(documentId, userId, "UpdateMetadata", "Succeeded", "Attached to task", cancellationToken);
        return true;
    }
    public async Task<List<DocumentSummary>> GetSharedDocumentsAsync(int userId, CancellationToken cancellationToken = default) => await _context.Documents.Where(d => d.Shares.Any(s => s.SharedWithUserId == userId)).Include(d => d.Project).Include(d => d.UploadedByUser).OrderByDescending(d => d.UploadedAt).Select(ToSummary()).ToListAsync(cancellationToken);

    public async Task<Stream?> OpenAuthorizedAsync(int userId, int documentId, bool preview, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || !await _authorization.CanViewAsync(document, userId, cancellationToken))
        {
            await RecordActivityAsync(documentId, userId, "Denied", "Denied", "Document unavailable", cancellationToken);
            return null;
        }
        await RecordActivityAsync(documentId, userId, preview ? "Preview" : "Download", "Succeeded", null, cancellationToken);
        return await _storage.DownloadAsync(document.FilePath, cancellationToken);
    }

    public async Task<bool> UpdateMetadataAsync(int userId, int documentId, DocumentMetadataChanges changes, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, userId, cancellationToken))
        {
            await RecordActivityAsync(documentId, userId, "UpdateMetadata", "Denied", "Metadata update denied", cancellationToken);
            return false;
        }
        ValidateMetadata(changes.Title, changes.Category);
        document.Title = changes.Title.Trim(); document.Description = changes.Description?.Trim(); document.Category = changes.Category; document.Tags = string.Join(", ", changes.Tags);
        await _context.SaveChangesAsync(cancellationToken); await RecordActivityAsync(documentId, userId, "UpdateMetadata", "Succeeded", null, cancellationToken); return true;
    }

    public async Task<bool> ReplaceFileAsync(int userId, int documentId, Stream content, string originalFileName, string contentType, long fileSize, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, userId, cancellationToken))
        {
            await RecordActivityAsync(documentId, userId, "Replace", "Denied", "Replacement denied", cancellationToken);
            return false;
        }
        var metadata = new DocumentUploadMetadata(document.Title, document.Description, document.Category, document.ProjectId, Array.Empty<string>(), Path.GetFileName(originalFileName), contentType, fileSize, document.TaskId);
        ValidateMetadata(metadata);
        var buffer = new MemoryStream(); await content.CopyToAsync(buffer, cancellationToken); buffer.Position = 0;
        var scan = await _scanner.ScanAsync(buffer, metadata.OriginalFileName, metadata.ContentType, cancellationToken);
        if (scan != MalwareScanResult.Clean) throw new DocumentUploadException(scan == MalwareScanResult.Infected ? DocumentUploadErrorCode.MalwareDetected : DocumentUploadErrorCode.ScanUnavailable, "The replacement file failed the security scan.");
        var newPath = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{Path.GetExtension(metadata.OriginalFileName).ToLowerInvariant()}";
        buffer.Position = 0; await _storage.UploadAsync(buffer, newPath, contentType, cancellationToken);
        var oldPath = document.FilePath;
        try { document.FilePath = newPath; document.OriginalFileName = metadata.OriginalFileName; document.FileType = contentType; document.FileSize = fileSize; await _context.SaveChangesAsync(cancellationToken); await _storage.DeleteAsync(oldPath, cancellationToken); await RecordActivityAsync(documentId, userId, "Replace", "Succeeded", null, cancellationToken); return true; }
        catch { await _storage.DeleteAsync(newPath, cancellationToken); throw; }
        finally { buffer.Dispose(); }
    }

    public async Task<bool> DeleteAsync(int userId, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, userId, cancellationToken)) { await RecordActivityAsync(documentId, userId, "Denied", "Denied", "Delete denied", cancellationToken); return false; }
        var path = document.FilePath; _context.Documents.Remove(document); await _context.SaveChangesAsync(cancellationToken); await _storage.DeleteAsync(path, cancellationToken); await RecordActivityAsync(documentId, userId, "Delete", "Succeeded", null, cancellationToken); return true;
    }

    public async Task<bool> ShareAsync(int userId, int documentId, IEnumerable<DocumentShareTarget> targets, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([documentId], cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, userId, cancellationToken))
        {
            await RecordActivityAsync(documentId, userId, "Share", "Denied", "Share denied", cancellationToken);
            return false;
        }
        var actor = await _context.Users.FindAsync([userId], cancellationToken);
        foreach (var target in targets.Where(t => t.UserId.HasValue || !string.IsNullOrWhiteSpace(t.Department)))
        {
            var exists = await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.SharedWithUserId == target.UserId && s.SharedWithDepartment == target.Department, cancellationToken);
            if (exists) continue;
            _context.DocumentShares.Add(new DocumentShare { DocumentId = documentId, SharedWithUserId = target.UserId, SharedWithDepartment = target.Department, SharedByUserId = userId });
            if (target.UserId.HasValue) await _notifications.NotifyDocumentSharedAsync(document, target.UserId.Value, actor?.DisplayName ?? "A user");
        }
        await _context.SaveChangesAsync(cancellationToken); await RecordActivityAsync(documentId, userId, "Share", "Succeeded", null, cancellationToken); return true;
    }

    public async Task<AdminDocumentReport?> GetActivityReportAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user?.Role != UserRole.Administrator) return null;
        var types = await _context.Documents.GroupBy(d => d.FileType).Select(g => new ValueTuple<string, int>(g.Key, g.Count())).ToListAsync(cancellationToken);
        var uploaders = await _context.Documents.GroupBy(d => d.UploadedByUser.DisplayName).Select(g => new ValueTuple<string, int>(g.Key, g.Count())).ToListAsync(cancellationToken);
        var actions = await _context.DocumentActivities.GroupBy(a => a.Action).Select(g => new ValueTuple<string, int>(g.Key, g.Count())).ToListAsync(cancellationToken);
        return new AdminDocumentReport(types, uploaders, actions);
    }

    private IQueryable<Document> AccessibleQuery(int userId) => _context.Documents.Include(d => d.Project).Include(d => d.UploadedByUser).Where(d => d.UploadedByUserId == userId || d.Project!.ProjectManagerId == userId || d.Project!.ProjectMembers.Any(m => m.UserId == userId) || d.Shares.Any(s => s.SharedWithUserId == userId));
    private static Expression<Func<Document, DocumentSummary>> ToSummary() => d => new DocumentSummary(d.DocumentId, d.Title, d.Category, d.UploadedAt, d.FileSize, d.FileType, d.Project == null ? null : d.Project.Name, d.UploadedByUser.DisplayName, d.Description, d.Tags);
    private async Task RecordActivityAsync(int? documentId, int? actorId, string action, string outcome, string? details, CancellationToken cancellationToken) { _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, ActorUserId = actorId, Action = action, Outcome = outcome, Details = details }); await _context.SaveChangesAsync(cancellationToken); }
    private void ValidateMetadata(DocumentUploadMetadata metadata) { ValidateMetadata(metadata.Title, metadata.Category); if (metadata.FileSize <= 0 || metadata.FileSize > _options.MaximumFileSize) throw new DocumentUploadException(DocumentUploadErrorCode.InvalidRequest, "The file size is invalid."); if (!DocumentFeatureOptions.AllowedMimeTypes.TryGetValue(Path.GetExtension(metadata.OriginalFileName), out var types) || !types.Contains(metadata.ContentType, StringComparer.OrdinalIgnoreCase)) throw new DocumentUploadException(DocumentUploadErrorCode.UnsupportedFileType, "The file type is not supported."); }
    private static void ValidateMetadata(string title, string category) { if (string.IsNullOrWhiteSpace(title)) throw new DocumentUploadException(DocumentUploadErrorCode.InvalidRequest, "A document title is required."); if (!DocumentFeatureOptions.Categories.Contains(category, StringComparer.Ordinal)) throw new DocumentUploadException(DocumentUploadErrorCode.InvalidRequest, "A valid document category is required."); }
}
