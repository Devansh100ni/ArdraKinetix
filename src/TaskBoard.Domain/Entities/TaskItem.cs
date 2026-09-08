using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class TaskItem : BaseEntity, ITenantEntity, ISoftDeletable
{
    public string TaskNumber { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? ParentTaskId { get; set; }
    public decimal? EstimatedHours { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? EndOn { get; set; }
    public Guid StatusId { get; set; }
    public Guid PriorityId { get; set; }
    
    public Guid CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }

    // Concurrency Token
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual User? AssignedUser { get; set; }
    public virtual User? Creator { get; set; }
    public virtual TaskItem? ParentTask { get; set; }
    public virtual ICollection<TaskItem> ChildTasks { get; set; } = new List<TaskItem>();
    public virtual TaskStatusItem Status { get; set; } = null!;
    public virtual TaskPriority Priority { get; set; } = null!;
    public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public virtual ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
    public virtual ICollection<TaskAudit> Audits { get; set; } = new List<TaskAudit>();
}
