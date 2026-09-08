using TaskBoard.Application.DTOs.Attachments;
using TaskBoard.Application.DTOs.Audit;
using TaskBoard.Application.DTOs.Comments;
using TaskBoard.Application.DTOs.Priorities;
using TaskBoard.Application.DTOs.Statuses;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.DTOs.Users;

namespace TaskBoard.Application.DTOs.Tasks;

public class TaskDto
{
    public Guid Id { get; set; }
    public string TaskNumber { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? TenantCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public Guid? ParentTaskId { get; set; }
    public string? ParentTaskNumber { get; set; }
    public string? ParentTaskTitle { get; set; }
    public decimal? EstimatedHours { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? EndOn { get; set; }
    public Guid StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public Guid PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public string PriorityColor { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int CommentCount { get; set; }
    public int AttachmentCount { get; set; }
    public int ChildTaskCount { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public class TaskDetailDto : TaskDto
{
    public TenantDto Tenant { get; set; } = null!;
    public UserDto? AssignedUser { get; set; }
    public UserDto? Creator { get; set; }
    public TaskStatusDto Status { get; set; } = null!;
    public TaskPriorityDto Priority { get; set; } = null!;
    public TaskDto? ParentTask { get; set; }
    public IReadOnlyList<TaskDto> ChildTasks { get; set; } = [];
    public IReadOnlyList<TaskCommentDto> Comments { get; set; } = [];
    public IReadOnlyList<TaskAttachmentDto> Attachments { get; set; } = [];
    public IReadOnlyList<TaskAuditDto> Audits { get; set; } = [];
}

public class CreateTaskDto
{
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
}

public class UpdateTaskDto
{
    public Guid Id { get; set; }
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
    public byte[] RowVersion { get; set; } = [];
}

public class MoveTaskDto
{
    public Guid TaskId { get; set; }
    public Guid TargetStatusId { get; set; }
    public byte[]? RowVersion { get; set; }
}
