using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.DTOs.Dashboard;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Exceptions;

namespace TaskBoard.Application.Features.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITaskStatusRepository _statusRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public DashboardService(
        ITaskRepository taskRepository,
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        ITaskStatusRepository statusRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _taskRepository = taskRepository;
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _statusRepository = statusRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        var allTenants = await _tenantRepository.GetAllActiveAsync(cancellationToken);
        var totalTasks = await _taskRepository.GetTotalCountAsync(null, null, cancellationToken);
        var tasksByStatus = await _taskRepository.GetTaskCountsByStatusAsync(null, null, cancellationToken);
        var tasksByPriority = await _taskRepository.GetTaskCountsByPriorityAsync(null, null, cancellationToken);
        var recentTasks = await _taskRepository.GetRecentTasksAsync(10, null, null, cancellationToken);
        var overdueTasks = await _taskRepository.GetOverdueTasksAsync(null, null, cancellationToken);
        var usersPaged = await _userRepository.GetPagedAsync(new Common.FilterRequest { PageSize = 1 }, null, cancellationToken);

        return new AdminDashboardDto
        {
            TotalTenants = allTenants.Count,
            ActiveUsers = usersPaged.TotalCount,
            TotalTasks = totalTasks,
            OverdueTasksCount = overdueTasks.Count,
            TasksByStatus = tasksByStatus,
            TasksByPriority = tasksByPriority,
            RecentTenants = allTenants.Take(5).Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                Prefix = t.Prefix,
                IsActive = t.IsActive,
                CreatedOn = t.CreatedOn
            }).ToList(),
            RecentTasks = recentTasks.Select(MapToTaskDto).ToList(),
            OverdueTasks = overdueTasks.Select(MapToTaskDto).ToList()
        };
    }

    public async Task<DeveloperDashboardDto> GetDeveloperDashboardAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var allowedTenantIds = _tenantContext.AllowedTenantIds;

        var myTasksPaged = await _taskRepository.GetPagedAsync(new TaskFilterCriteria
        {
            AssignedUserId = currentUserId,
            AllowedTenantIds = allowedTenantIds,
            PageSize = 50
        }, cancellationToken);

        var tasksByStatus = await _taskRepository.GetTaskCountsByStatusAsync(null, allowedTenantIds, cancellationToken);
        var recentTasks = await _taskRepository.GetRecentTasksAsync(8, null, allowedTenantIds, cancellationToken);
        var overdueTasks = await _taskRepository.GetOverdueTasksAsync(null, allowedTenantIds, cancellationToken);

        var myOverdue = overdueTasks.Where(t => t.AssignedUserId == currentUserId).ToList();
        var myHighPriority = myTasksPaged.Items.Where(t => t.Priority?.Code == "HIGH" || t.Priority?.Code == "CRITICAL").ToList();

        var tenants = new List<TenantDto>();
        foreach (var tId in allowedTenantIds)
        {
            var t = await _tenantRepository.GetByIdAsync(tId, cancellationToken);
            if (t != null)
            {
                tenants.Add(new TenantDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Code = t.Code,
                    Prefix = t.Prefix,
                    IsActive = t.IsActive,
                    CreatedOn = t.CreatedOn
                });
            }
        }

        return new DeveloperDashboardDto
        {
            AssignedTenantsCount = allowedTenantIds.Count,
            MyAssignedTasksCount = myTasksPaged.TotalCount,
            HighPriorityTasksCount = myHighPriority.Count,
            OverdueTasksCount = myOverdue.Count,
            AssignedTenants = tenants,
            TasksByStatus = tasksByStatus,
            MyTasks = myTasksPaged.Items.Take(10).Select(MapToTaskDto).ToList(),
            RecentTasks = recentTasks.Select(MapToTaskDto).ToList()
        };
    }

    public async Task<TenantDashboardDto> GetTenantDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new TenantAccessDeniedException("No tenant context found for tenant user.");
        }

        var tenantId = _tenantContext.TenantId.Value;
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new TenantAccessDeniedException("Tenant not found.");
        }

        var totalTasks = await _taskRepository.GetTotalCountAsync(tenantId, null, cancellationToken);
        var tasksByStatus = await _taskRepository.GetTaskCountsByStatusAsync(tenantId, null, cancellationToken);
        var tasksByPriority = await _taskRepository.GetTaskCountsByPriorityAsync(tenantId, null, cancellationToken);
        var recentTasks = await _taskRepository.GetRecentTasksAsync(10, tenantId, null, cancellationToken);
        var overdueTasks = await _taskRepository.GetOverdueTasksAsync(tenantId, null, cancellationToken);

        int doneCount = 0;
        if (tasksByStatus.TryGetValue("Done", out var dCount))
        {
            doneCount = dCount;
        }

        return new TenantDashboardDto
        {
            TenantName = tenant.Name,
            TenantCode = tenant.Code,
            TotalTasks = totalTasks,
            OpenTasks = Math.Max(0, totalTasks - doneCount),
            CompletedTasks = doneCount,
            OverdueTasksCount = overdueTasks.Count,
            TasksByStatus = tasksByStatus,
            TasksByPriority = tasksByPriority,
            RecentTasks = recentTasks.Select(MapToTaskDto).ToList()
        };
    }

    private static TaskDto MapToTaskDto(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            TaskNumber = task.TaskNumber,
            TenantId = task.TenantId,
            TenantName = task.Tenant?.Name,
            TenantCode = task.Tenant?.Code,
            Title = task.Title,
            Description = task.Description,
            AssignedUserId = task.AssignedUserId,
            AssignedUserName = task.AssignedUser?.FullName,
            ParentTaskId = task.ParentTaskId,
            ParentTaskNumber = task.ParentTask?.TaskNumber,
            EstimatedHours = task.EstimatedHours,
            StartedOn = task.StartedOn,
            EndOn = task.EndOn,
            StatusId = task.StatusId,
            StatusName = task.Status?.Name ?? "Unknown",
            StatusColor = task.Status?.Color ?? "#64748b",
            PriorityId = task.PriorityId,
            PriorityName = task.Priority?.Name ?? "Normal",
            PriorityColor = task.Priority?.Color ?? "#64748b",
            CreatedBy = task.CreatedBy,
            CreatedByName = task.Creator?.FullName,
            CreatedOn = task.CreatedOn,
            CommentCount = task.Comments?.Count(c => !c.IsDeleted) ?? 0,
            AttachmentCount = task.Attachments?.Count(a => !a.IsDeleted) ?? 0,
            ChildTaskCount = task.ChildTasks?.Count(ct => !ct.IsDeleted) ?? 0,
            RowVersion = task.RowVersion
        };
    }
}
