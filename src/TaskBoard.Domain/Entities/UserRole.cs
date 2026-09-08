namespace TaskBoard.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public DateTime AssignedOn { get; set; } = DateTime.UtcNow;
}
