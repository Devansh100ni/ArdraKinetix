using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class Tenant : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public virtual ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<ScrumBoard> ScrumBoards { get; set; } = new List<ScrumBoard>();
    public virtual TenantTaskSequence? TaskSequence { get; set; }
}
