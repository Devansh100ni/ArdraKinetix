using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class ScrumBoardColumn : AuditableEntity, ISoftDeletable
{
    public Guid BoardId { get; set; }
    public virtual ScrumBoard Board { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public Guid StatusId { get; set; }
    public virtual TaskStatusItem Status { get; set; } = null!;

    public int? WipLimit { get; set; }
    public bool IsActive { get; set; } = true;

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }
}
