namespace TaskBoard.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOn { get; set; }
}
