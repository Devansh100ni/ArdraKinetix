using TaskBoard.Application.Common;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<Tenant>> GetPagedAsync(FilterRequest request, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
    void Update(Tenant tenant);
}
