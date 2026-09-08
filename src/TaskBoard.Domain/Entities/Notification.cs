using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "General"; // 'TaskAssigned', 'TaskStatusChanged', 'CommentAdded', 'SecurityAlert'
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadOn { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual User User { get; set; } = null!;
    public virtual Tenant? Tenant { get; set; }
}
