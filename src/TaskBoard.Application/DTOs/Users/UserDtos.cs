using TaskBoard.Application.DTOs.Tenants;

namespace TaskBoard.Application.DTOs.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastLoginOn { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public IReadOnlyList<TenantDto> AssignedTenants { get; set; } = [];
}

public class CreateUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<Guid> AssignedTenantIds { get; set; } = [];
    public bool IsActive { get; set; } = true;
}

public class UpdateUserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? NewPassword { get; set; }
    public string Role { get; set; } = string.Empty;
    public List<Guid> AssignedTenantIds { get; set; } = [];
    public bool IsActive { get; set; }
}

public class AssignUserTenantsDto
{
    public Guid UserId { get; set; }
    public List<Guid> TenantIds { get; set; } = [];
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
