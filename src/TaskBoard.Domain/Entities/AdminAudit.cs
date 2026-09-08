using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class AdminAudit : BaseEntity
{
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
