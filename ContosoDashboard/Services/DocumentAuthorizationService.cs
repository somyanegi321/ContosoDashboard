using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentAuthorizationService
{
    private readonly ApplicationDbContext _context;
    public DocumentAuthorizationService(ApplicationDbContext context) => _context = context;

    public async Task<bool> CanViewAsync(Document document, int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null) return false;
        if (user.Role == UserRole.Administrator || document.UploadedByUserId == userId) return true;
        if (document.ProjectId.HasValue && await IsProjectMemberAsync(document.ProjectId.Value, userId, cancellationToken)) return true;
        return await _context.DocumentShares.AnyAsync(s => s.DocumentId == document.DocumentId &&
            ((s.SharedWithUserId.HasValue && s.SharedWithUserId == userId) ||
             (s.SharedWithDepartment != null && s.SharedWithDepartment == user.Department)), cancellationToken);
    }

    public async Task<bool> CanManageAsync(Document document, int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null) return false;
        if (user.Role == UserRole.Administrator || document.UploadedByUserId == userId) return true;
        return document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId, cancellationToken);
    }

    public Task<bool> CanViewProjectAsync(int projectId, int userId, CancellationToken cancellationToken = default)
        => IsProjectMemberAsync(projectId, userId, cancellationToken);

    private Task<bool> IsProjectMemberAsync(int projectId, int userId, CancellationToken cancellationToken)
        => _context.Projects.AnyAsync(p => p.ProjectId == projectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(m => m.UserId == userId)), cancellationToken);
}
