using TaskBoard.Application.Common;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailOrUsernameAsync(string identifier, CancellationToken cancellationToken = default);
    Task<User?> GetWithRolesAndTenantsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<User>> GetPagedAsync(FilterRequest request, Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetUsersByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsEmailAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsUsernameAsync(string username, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task AssignToTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
    Task RemoveFromTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}
