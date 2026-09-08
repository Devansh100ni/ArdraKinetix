namespace TaskBoard.Domain.Entities;

public class TenantTaskSequence
{
    public Guid TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;

    public long LastNumber { get; set; } = 0;
}
