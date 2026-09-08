using TaskBoard.Application.Common;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Repositories;

public interface ITaskAuditRepository
{
    Task<IReadOnlyList<TaskAudit>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<PagedResult<TaskAudit>> GetPagedAsync(Guid? taskId, FilterRequest request, CancellationToken cancellationToken = default);
    Task AddAsync(TaskAudit audit, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TaskAudit> audits, CancellationToken cancellationToken = default);
}
