using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresUtc { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedOn { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? ReasonRevoked { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
    public bool IsRevoked => RevokedOn != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation
    public virtual User User { get; set; } = null!;
}
