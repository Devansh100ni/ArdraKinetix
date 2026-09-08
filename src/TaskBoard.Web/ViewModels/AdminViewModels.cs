using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Audit;
using TaskBoard.Application.DTOs.Priorities;
using TaskBoard.Application.DTOs.Scrum;
using TaskBoard.Application.DTOs.Statuses;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.DTOs.Users;

namespace TaskBoard.Web.ViewModels;

public class AdminTenantsViewModel
{
    public PagedResult<TenantDto> Tenants { get; set; } = PagedResult<TenantDto>.Empty();
    public FilterRequest Filter { get; set; } = new();
}

public class AdminUsersViewModel
{
    public PagedResult<UserDto> Users { get; set; } = PagedResult<UserDto>.Empty();
    public FilterRequest Filter { get; set; } = new();
    public Guid? SelectedTenantId { get; set; }
    public IReadOnlyList<TenantDto> AvailableTenants { get; set; } = [];
}

public class AdminUserCreateEditViewModel
{
    public Guid? Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string Role { get; set; } = string.Empty;
    public List<Guid> AssignedTenantIds { get; set; } = [];
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<RoleDto> AvailableRoles { get; set; } = [];
    public IReadOnlyList<TenantDto> AvailableTenants { get; set; } = [];
    public bool IsEdit => Id.HasValue && Id.Value != Guid.Empty;
}

public class AdminAuditViewModel
{
    public PagedResult<AdminAuditDto> AdminAudits { get; set; } = PagedResult<AdminAuditDto>.Empty();
    public PagedResult<TaskAuditDto> TaskAudits { get; set; } = PagedResult<TaskAuditDto>.Empty();
    public FilterRequest Filter { get; set; } = new();
    public string ActiveTab { get; set; } = "system";
}
