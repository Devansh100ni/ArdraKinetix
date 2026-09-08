using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Audit;

namespace TaskBoard.Application.Features.Audit;

public interface IAuditService
{
    Task<PagedResult<TaskAuditDto>> GetTaskAuditsPagedAsync(Guid? taskId, FilterRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<AdminAuditDto>> GetAdminAuditsPagedAsync(FilterRequest request, string? entityType = null, CancellationToken cancellationToken = default);
}
