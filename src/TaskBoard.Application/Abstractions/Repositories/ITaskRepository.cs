using TaskBoard.Application.Common;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Repositories;

public class TaskFilterCriteria : FilterRequest
{
    public Guid? TenantId { get; set; }
    public IReadOnlyList<Guid>? AllowedTenantIds { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? PriorityId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public Guid? ParentTaskId { get; set; }
    public bool OnlyParentTasks { get; set; }
    public DateTime? FromCreatedDate { get; set; }
    public DateTime? ToCreatedDate { get; set; }
    public DateTime? FromStartedDate { get; set; }
    public DateTime? ToStartedDate { get; set; }
    public DateTime? FromEndDate { get; set; }
    public DateTime? ToEndDate { get; set; }
}

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByNumberAsync(string taskNumber, Guid tenantId, CancellationToken cancellationToken = default);
    Task<PagedResult<TaskItem>> GetPagedAsync(TaskFilterCriteria criteria, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetBoardTasksAsync(Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetChildTasksAsync(Guid parentTaskId, CancellationToken cancellationToken = default);
    Task<bool> HasCircularDependencyAsync(Guid taskId, Guid prospectiveParentId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    void Update(TaskItem task);
    
    // Comments
    Task<TaskComment?> GetCommentByIdAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task AddCommentAsync(TaskComment comment, CancellationToken cancellationToken = default);
    void UpdateComment(TaskComment comment);

    // Attachments
    Task<TaskAttachment?> GetAttachmentByIdAsync(Guid attachmentId, CancellationToken cancellationToken = default);
    Task AddAttachmentAsync(TaskAttachment attachment, CancellationToken cancellationToken = default);
    void UpdateAttachment(TaskAttachment attachment);

    // Metrics for Dashboards
    Task<int> GetTotalCountAsync(Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default);
    Task<int> GetCountByStatusAsync(Guid statusId, Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetTaskCountsByStatusAsync(Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetTaskCountsByPriorityAsync(Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetRecentTasksAsync(int count, Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetOverdueTasksAsync(Guid? tenantId, IReadOnlyList<Guid>? allowedTenantIds, CancellationToken cancellationToken = default);
}
