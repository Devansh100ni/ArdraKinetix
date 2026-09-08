using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Users;

namespace TaskBoard.Application.Features.Users;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<UserDto>> GetPagedAsync(FilterRequest request, Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetUsersByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateAsync(UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<Result> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> ToggleLockAsync(Guid id, string? reason = null, CancellationToken cancellationToken = default);
    Task<Result> ForcePasswordResetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> RevokeSessionsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<Result> AssignTenantsAsync(AssignUserTenantsDto dto, CancellationToken cancellationToken = default);
}
