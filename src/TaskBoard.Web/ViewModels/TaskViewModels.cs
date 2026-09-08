using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Priorities;
using TaskBoard.Application.DTOs.Statuses;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.DTOs.Users;

namespace TaskBoard.Web.ViewModels;

public class TaskListViewModel
{
    public PagedResult<TaskDto> Tasks { get; set; } = PagedResult<TaskDto>.Empty();
    public TaskFilterCriteria Criteria { get; set; } = new();
    public IReadOnlyList<TenantDto> AvailableTenants { get; set; } = [];
    public IReadOnlyList<TaskStatusDto> AvailableStatuses { get; set; } = [];
    public IReadOnlyList<TaskPriorityDto> AvailablePriorities { get; set; } = [];
    public IReadOnlyList<UserDto> AvailableUsers { get; set; } = [];
    public bool CanCreateTask { get; set; }
}

public class TaskCreateEditViewModel
{
    public Guid? Id { get; set; }
    public string? TaskNumber { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? ParentTaskId { get; set; }
    public decimal? EstimatedHours { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? EndOn { get; set; }
    public Guid StatusId { get; set; }
    public Guid PriorityId { get; set; }
    public byte[]? RowVersion { get; set; }

    public IReadOnlyList<TenantDto> AvailableTenants { get; set; } = [];
    public IReadOnlyList<TaskStatusDto> AvailableStatuses { get; set; } = [];
    public IReadOnlyList<TaskPriorityDto> AvailablePriorities { get; set; } = [];
    public IReadOnlyList<UserDto> AvailableUsers { get; set; } = [];
    public IReadOnlyList<TaskDto> AvailableParentTasks { get; set; } = [];

    public bool IsEdit => Id.HasValue && Id.Value != Guid.Empty;
}

public class TaskDetailsViewModel
{
    public TaskDetailDto Task { get; set; } = null!;
    public string ActiveTab { get; set; } = "overview";
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanComment { get; set; }
    public bool CanUpload { get; set; }
}
