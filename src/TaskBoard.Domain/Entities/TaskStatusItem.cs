using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class TaskStatusItem : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string Color { get; set; } = "#64748b";
    public bool IsActive { get; set; } = true;

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }

    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<ScrumBoardColumn> BoardColumns { get; set; } = new List<ScrumBoardColumn>();
}
