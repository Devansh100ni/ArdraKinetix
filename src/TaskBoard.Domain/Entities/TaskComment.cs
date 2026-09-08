using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class TaskComment : AuditableEntity, ISoftDeletable
{
    public Guid TaskId { get; set; }
    public virtual TaskItem Task { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string Comment { get; set; } = string.Empty;

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }
}
