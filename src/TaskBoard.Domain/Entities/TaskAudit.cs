using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class TaskAudit : BaseEntity
{
    public Guid TaskId { get; set; }
    public virtual TaskItem Task { get; set; } = null!;

    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
