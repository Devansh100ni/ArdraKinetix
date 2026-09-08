using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Audit;

namespace TaskBoard.Application.Features.Audit;

public class AuditService : IAuditService
{
    private readonly ITaskAuditRepository _taskAuditRepository;
    private readonly IAdminAuditRepository _adminAuditRepository;

    public AuditService(ITaskAuditRepository taskAuditRepository, IAdminAuditRepository adminAuditRepository)
    {
        _taskAuditRepository = taskAuditRepository;
        _adminAuditRepository = adminAuditRepository;
    }

    public async Task<PagedResult<TaskAuditDto>> GetTaskAuditsPagedAsync(Guid? taskId, FilterRequest request, CancellationToken cancellationToken = default)
    {
        var paged = await _taskAuditRepository.GetPagedAsync(taskId, request, cancellationToken);
        var dtos = paged.Items.Select(a => new TaskAuditDto
        {
            Id = a.Id,
            TaskId = a.TaskId,
            TaskNumber = a.Task?.TaskNumber ?? "",
            UserId = a.UserId,
            UserName = a.User?.Username ?? "System",
            UserFullName = a.User?.FullName ?? "System",
            Action = a.Action,
            FieldName = a.FieldName,
            OldValue = a.OldValue,
            NewValue = a.NewValue,
            CreatedOn = a.CreatedOn,
            IpAddress = a.IpAddress
        }).ToList();

        return new PagedResult<TaskAuditDto>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    public async Task<PagedResult<AdminAuditDto>> GetAdminAuditsPagedAsync(FilterRequest request, string? entityType = null, CancellationToken cancellationToken = default)
    {
        var paged = await _adminAuditRepository.GetPagedAsync(request, entityType, cancellationToken);
        var dtos = paged.Items.Select(a => new AdminAuditDto
        {
            Id = a.Id,
            UserId = a.UserId,
            UserName = a.User?.Username ?? "System",
            UserFullName = a.User?.FullName ?? "System",
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            Details = a.Details,
            CreatedOn = a.CreatedOn,
            IpAddress = a.IpAddress
        }).ToList();

        return new PagedResult<AdminAuditDto>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }
}
