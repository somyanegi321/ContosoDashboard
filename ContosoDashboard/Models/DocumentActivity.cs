using System.ComponentModel.DataAnnotations;

namespace ContosoDashboard.Models;

public class DocumentActivity
{
    [Key] public int DocumentActivityId { get; set; }
    public int? DocumentId { get; set; }
    public int? ActorUserId { get; set; }
    [Required, MaxLength(50)] public string Action { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Outcome { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string? Details { get; set; }
    public virtual Document? Document { get; set; }
    public virtual User? ActorUser { get; set; }
}
