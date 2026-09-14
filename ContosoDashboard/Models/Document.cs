using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public class Document
{
    [Key] public int DocumentId { get; set; }
    [Required, MaxLength(255)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    [Required, MaxLength(100)] public string Category { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Tags { get; set; }
    [Required, MaxLength(512)] public string OriginalFileName { get; set; } = string.Empty;
    [Required, MaxLength(512)] public string FilePath { get; set; } = string.Empty;
    [Range(1, 26214400)] public long FileSize { get; set; }
    [Required, MaxLength(255)] public string FileType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int UploadedByUserId { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public virtual User UploadedByUser { get; set; } = null!;
    public virtual Project? Project { get; set; }
    public virtual TaskItem? Task { get; set; }
    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    public virtual ICollection<DocumentActivity> Activities { get; set; } = new List<DocumentActivity>();
}
