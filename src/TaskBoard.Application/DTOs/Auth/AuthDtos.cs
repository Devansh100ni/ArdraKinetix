using TaskBoard.Application.DTOs.Tenants;

namespace TaskBoard.Application.DTOs.Auth;

public class LoginRequest
{
    public string Identifier { get; set; } = string.Empty; // Email or Username
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresUtc { get; set; }
    public string? Error { get; set; }
    public bool MustChangePassword { get; set; }
    public bool IsLockedOut { get; set; }
    public int? LockoutMinutesRemaining { get; set; }
    public CurrentUserDto? User { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string? TenantName { get; set; }
    public IReadOnlyList<Guid> AllowedTenantIds { get; set; } = [];
    public IReadOnlyList<TenantDto> AllowedTenants { get; set; } = [];
}
