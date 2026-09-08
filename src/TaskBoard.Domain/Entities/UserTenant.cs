namespace TaskBoard.Domain.Entities;

public class UserTenant
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public Guid TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    public DateTime AssignedOn { get; set; } = DateTime.UtcNow;
}
