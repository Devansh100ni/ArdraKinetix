using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class ScrumBoard : AuditableEntity, ISoftDeletable
{
    // TenantId is nullable: null indicates global board template, non-null indicates custom tenant board
    public Guid? TenantId { get; set; }
    public virtual Tenant? Tenant { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }

    public virtual ICollection<ScrumBoardColumn> Columns { get; set; } = new List<ScrumBoardColumn>();
}
