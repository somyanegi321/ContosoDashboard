using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetDashboardSummaryAsync(int userId);
    Task<List<Announcement>> GetActiveAnnouncementsAsync();
    Task<List<DocumentSummary>> GetRecentDocumentsAsync(int userId);
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(int userId)
    {
        var now = DateTime.UtcNow;

        var summary = new DashboardSummary
        {
            TotalActiveTasks = await _context.Tasks
                .CountAsync(t => t.AssignedUserId == userId && t.Status != Models.TaskStatus.Completed),

            TasksDueToday = await _context.Tasks
                .CountAsync(t => t.AssignedUserId == userId 
                    && t.DueDate.HasValue 
                    && t.DueDate.Value.Date == now.Date
                    && t.Status != Models.TaskStatus.Completed),

            ActiveProjects = await _context.Projects
                .Where(p => p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId))
                .Where(p => p.Status == ProjectStatus.Active)
                .CountAsync(),

            UnreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead),
            DocumentCount = await _context.Documents.CountAsync(d => d.UploadedByUserId == userId)
        };

        return summary;
    }

    public Task<List<DocumentSummary>> GetRecentDocumentsAsync(int userId) => _context.Documents
        .Where(d => d.UploadedByUserId == userId)
        .Include(d => d.Project).Include(d => d.UploadedByUser)
        .OrderByDescending(d => d.UploadedAt).Take(5)
        .Select(d => new DocumentSummary(d.DocumentId, d.Title, d.Category, d.UploadedAt, d.FileSize, d.FileType, d.Project == null ? null : d.Project.Name, d.UploadedByUser.DisplayName, d.Description, d.Tags))
        .ToListAsync();

    public async Task<List<Announcement>> GetActiveAnnouncementsAsync()
    {
        var now = DateTime.UtcNow;

        return await _context.Announcements
            .Include(a => a.CreatedByUser)
            .Where(a => a.IsActive 
                && a.PublishDate <= now 
                && (!a.ExpiryDate.HasValue || a.ExpiryDate.Value > now))
            .OrderByDescending(a => a.PublishDate)
            .Take(5)
            .ToListAsync();
    }
}

public class DashboardSummary
{
    public int TotalActiveTasks { get; set; }
    public int TasksDueToday { get; set; }
    public int ActiveProjects { get; set; }
    public int UnreadNotifications { get; set; }
    public int DocumentCount { get; set; }
}
