using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class User : AuditableEntity, ISoftDeletable
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginOn { get; set; }

    public string FullName => string.IsNullOrWhiteSpace(LastName) 
        ? FirstName 
        : $"{FirstName} {LastName}".Trim();

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
    public virtual ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public virtual ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
    public virtual ICollection<TaskAudit> Audits { get; set; } = new List<TaskAudit>();
}
