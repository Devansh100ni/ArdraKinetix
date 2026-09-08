using TaskBoard.Application.Common;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Repositories;

public interface IAdminAuditRepository
{
    Task<PagedResult<AdminAudit>> GetPagedAsync(FilterRequest request, string? entityType = null, CancellationToken cancellationToken = default);
    Task AddAsync(AdminAudit audit, CancellationToken cancellationToken = default);
}
