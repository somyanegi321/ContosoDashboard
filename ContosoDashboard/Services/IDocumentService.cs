namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<DocumentUploadResult> UploadAsync(
        int requestingUserId,
        DocumentUploadMetadata metadata,
        Stream content,
        CancellationToken cancellationToken = default);
    Task<DocumentPage> GetMyDocumentsAsync(int userId, DocumentQuery query, CancellationToken cancellationToken = default);
    Task<DocumentPage> SearchAsync(int userId, DocumentQuery query, CancellationToken cancellationToken = default);
    Task<List<DocumentSummary>> GetProjectDocumentsAsync(int userId, int projectId, CancellationToken cancellationToken = default);
    Task<List<DocumentSummary>> GetTaskDocumentsAsync(int userId, int taskId, CancellationToken cancellationToken = default);
    Task<bool> AttachToTaskAsync(int userId, int documentId, int taskId, CancellationToken cancellationToken = default);
    Task<List<DocumentSummary>> GetSharedDocumentsAsync(int userId, CancellationToken cancellationToken = default);
    Task<Stream?> OpenAuthorizedAsync(int userId, int documentId, bool preview, CancellationToken cancellationToken = default);
    Task<bool> UpdateMetadataAsync(int userId, int documentId, DocumentMetadataChanges changes, CancellationToken cancellationToken = default);
    Task<bool> ReplaceFileAsync(int userId, int documentId, Stream content, string originalFileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int userId, int documentId, CancellationToken cancellationToken = default);
    Task<bool> ShareAsync(int userId, int documentId, IEnumerable<DocumentShareTarget> targets, CancellationToken cancellationToken = default);
    Task<AdminDocumentReport?> GetActivityReportAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed record DocumentUploadMetadata(
    string Title,
    string? Description,
    string Category,
    int? ProjectId,
    IReadOnlyCollection<string> Tags,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    int? TaskId = null);

public sealed record DocumentUploadResult(int DocumentId, string Title);

public enum DocumentUploadErrorCode
{
    InvalidRequest,
    FileTooLarge,
    UnsupportedFileType,
    MalwareDetected,
    ScanUnavailable,
    Unauthorized,
    StorageFailure
}

public sealed class DocumentUploadException : Exception
{
    public DocumentUploadException(DocumentUploadErrorCode code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public DocumentUploadErrorCode Code { get; }
}
