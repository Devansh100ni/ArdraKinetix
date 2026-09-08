using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Attachments;
using TaskBoard.Application.DTOs.Audit;
using TaskBoard.Application.DTOs.Comments;
using TaskBoard.Application.DTOs.Priorities;
using TaskBoard.Application.DTOs.Statuses;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using TaskBoard.Domain.Exceptions;

namespace TaskBoard.Application.Features.Tasks;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITaskStatusRepository _statusRepository;
    private readonly ITaskPriorityRepository _priorityRepository;
    private readonly ITaskAuditRepository _taskAuditRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITaskNumberGenerator _taskNumberGenerator;
    private readonly IFileStorageService _fileStorageService;
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly TaskBoard.Application.Features.Notifications.INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository taskRepository,
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        ITaskStatusRepository statusRepository,
        ITaskPriorityRepository priorityRepository,
        ITaskAuditRepository taskAuditRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        ITaskNumberGenerator taskNumberGenerator,
        IFileStorageService fileStorageService,
        IRealtimeNotificationService realtimeService,
        TaskBoard.Application.Features.Notifications.INotificationService notificationService,
        IUnitOfWork unitOfWork,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _statusRepository = statusRepository;
        _priorityRepository = priorityRepository;
        _taskAuditRepository = taskAuditRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _taskNumberGenerator = taskNumberGenerator;
        _fileStorageService = fileStorageService;
        _realtimeService = realtimeService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TaskDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (task == null || task.IsDeleted)
        {
            return null;
        }

        // Strict Tenant Isolation Verification
        if (!_tenantContext.IsGlobalAdmin && !_tenantContext.HasAccessToTenant(task.TenantId))
        {
            _logger.LogWarning("Unauthorized access attempt to task {TaskId} belonging to tenant {TenantId} by user {UserId}",
                id, task.TenantId, _currentUserService.UserId);
            throw new TenantAccessDeniedException(task.TenantId, _tenantContext.TenantId);
        }

        return MapToDetailDto(task, _currentUserService.UserId);
    }

    public async Task<PagedResult<TaskDto>> GetPagedAsync(TaskFilterCriteria criteria, CancellationToken cancellationToken = default)
    {
        // Enforce tenant scoping based on user context
        if (!_tenantContext.IsGlobalAdmin)
        {
            if (_currentUserService.IsInRole(SystemRoles.TenantUser))
            {
                criteria.TenantId = _tenantContext.TenantId;
            }
            else if (_currentUserService.IsInRole(SystemRoles.Developer))
            {
                if (criteria.TenantId.HasValue && !_tenantContext.HasAccessToTenant(criteria.TenantId.Value))
                {
                    throw new TenantAccessDeniedException(criteria.TenantId.Value, _tenantContext.TenantId);
                }
                criteria.AllowedTenantIds = _tenantContext.AllowedTenantIds;
            }
        }

        var pagedTasks = await _taskRepository.GetPagedAsync(criteria, cancellationToken);
        var dtos = pagedTasks.Items.Select(MapToDto).ToList();
        return new PagedResult<TaskDto>(dtos, pagedTasks.TotalCount, pagedTasks.PageNumber, pagedTasks.PageSize);
    }

    public async Task<Result<TaskDetailDto>> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default)
    {
        // Validate Tenant Access
        if (!_tenantContext.IsGlobalAdmin && !_tenantContext.HasAccessToTenant(dto.TenantId))
        {
            throw new TenantAccessDeniedException(dto.TenantId);
        }

        var tenant = await _tenantRepository.GetByIdAsync(dto.TenantId, cancellationToken);
        if (tenant == null || !tenant.IsActive)
        {
            return Result<TaskDetailDto>.Failure("Selected tenant is invalid or inactive.");
        }

        var status = await _statusRepository.GetByIdAsync(dto.StatusId, cancellationToken);
        if (status == null || !status.IsActive)
        {
            return Result<TaskDetailDto>.Failure("Selected status is invalid or inactive.");
        }

        var priority = await _priorityRepository.GetByIdAsync(dto.PriorityId, cancellationToken);
        if (priority == null || !priority.IsActive)
        {
            return Result<TaskDetailDto>.Failure("Selected priority is invalid or inactive.");
        }

        // Validate Assignee
        if (dto.AssignedUserId.HasValue)
        {
            var assignee = await _userRepository.GetWithRolesAndTenantsAsync(dto.AssignedUserId.Value, cancellationToken);
            if (assignee == null || !assignee.IsActive)
            {
                return Result<TaskDetailDto>.Failure("Assigned user is invalid or inactive.");
            }
        }

        // Validate Parent Task
        if (dto.ParentTaskId.HasValue)
        {
            var parent = await _taskRepository.GetByIdAsync(dto.ParentTaskId.Value, cancellationToken);
            if (parent == null || parent.IsDeleted || parent.TenantId != dto.TenantId)
            {
                return Result<TaskDetailDto>.Failure("Parent task must belong to the same tenant and be active.");
            }
        }

        // Generate concurrency-safe sequential task number
        var taskNumber = await _taskNumberGenerator.GenerateNextTaskNumberAsync(tenant.Id, tenant.Prefix, cancellationToken);

        var currentUserId = _currentUserService.UserId ?? Guid.Empty;

        var task = new TaskItem
        {
            TaskNumber = taskNumber,
            TenantId = dto.TenantId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            AssignedUserId = dto.AssignedUserId,
            ParentTaskId = dto.ParentTaskId,
            EstimatedHours = dto.EstimatedHours,
            StartedOn = dto.StartedOn,
            EndOn = dto.EndOn,
            StatusId = dto.StatusId,
            PriorityId = dto.PriorityId,
            CreatedBy = currentUserId,
            CreatedOn = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task, cancellationToken);

        // Record Audit for Creation
        var audit = new TaskAudit
        {
            TaskId = task.Id,
            UserId = currentUserId,
            Action = AuditActionType.Created.ToString(),
            FieldName = "Task",
            NewValue = $"Task '{task.TaskNumber}' created with title '{task.Title}'",
            CreatedOn = DateTime.UtcNow,
            IpAddress = _currentUserService.IpAddress
        };
        await _taskAuditRepository.AddAsync(audit, cancellationToken);

        if (dto.ParentTaskId.HasValue)
        {
            await _taskAuditRepository.AddAsync(new TaskAudit
            {
                TaskId = dto.ParentTaskId.Value,
                UserId = currentUserId,
                Action = AuditActionType.ChildTaskCreated.ToString(),
                FieldName = "ChildTasks",
                NewValue = $"Child task '{task.TaskNumber}' created",
                CreatedOn = DateTime.UtcNow,
                IpAddress = _currentUserService.IpAddress
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Task {TaskNumber} ({TaskId}) created for tenant {TenantId}.", task.TaskNumber, task.Id, task.TenantId);

        // Notify assigned user
        if (task.AssignedUserId.HasValue && task.AssignedUserId != currentUserId)
        {
            await _notificationService.CreateNotificationAsync(
                task.AssignedUserId.Value,
                task.TenantId,
                "New Task Assigned",
                $"You have been assigned to task {task.TaskNumber}: {task.Title}",
                "TaskAssigned",
                $"/Tasks/Details/{task.Id}",
                cancellationToken);
        }

        var loaded = await _taskRepository.GetByIdWithDetailsAsync(task.Id, cancellationToken);
        return Result<TaskDetailDto>.Success(MapToDetailDto(loaded ?? task, currentUserId));
    }

    public async Task<Result<TaskDetailDto>> UpdateAsync(UpdateTaskDto dto, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdWithDetailsAsync(dto.Id, cancellationToken);
        if (task == null || task.IsDeleted)
        {
            return Result<TaskDetailDto>.Failure("Task not found.");
        }

        // Validate Tenant Authorization
        if (!_tenantContext.IsGlobalAdmin && !_tenantContext.HasAccessToTenant(task.TenantId))
        {
            throw new TenantAccessDeniedException(task.TenantId, _tenantContext.TenantId);
        }

        var status = await _statusRepository.GetByIdAsync(dto.StatusId, cancellationToken);
        if (status == null || !status.IsActive)
        {
            return Result<TaskDetailDto>.Failure("Selected status is invalid or inactive.");
        }

        var priority = await _priorityRepository.GetByIdAsync(dto.PriorityId, cancellationToken);
        if (priority == null || !priority.IsActive)
        {
            return Result<TaskDetailDto>.Failure("Selected priority is invalid or inactive.");
        }

        // Validate Parent Task & Circular Hierarchy
        if (dto.ParentTaskId.HasValue)
        {
            if (dto.ParentTaskId.Value == task.Id)
            {
                return Result<TaskDetailDto>.Failure("A task cannot be its own parent.");
            }

            var parent = await _taskRepository.GetByIdAsync(dto.ParentTaskId.Value, cancellationToken);
            if (parent == null || parent.IsDeleted || parent.TenantId != task.TenantId)
            {
                return Result<TaskDetailDto>.Failure("Parent task must belong to the same tenant.");
            }

            if (await _taskRepository.HasCircularDependencyAsync(task.Id, dto.ParentTaskId.Value, cancellationToken))
            {
                return Result<TaskDetailDto>.Failure("Circular parent-child relationship detected.");
            }
        }

        var currentUserId = _currentUserService.UserId;
        var audits = new List<TaskAudit>();

        // Check field-level diffs for audit
        if (task.Title != dto.Title.Trim())
        {
            audits.Add(CreateAudit(task.Id, currentUserId, "Title", task.Title, dto.Title.Trim()));
            task.Title = dto.Title.Trim();
        }

        if (task.Description != dto.Description?.Trim())
        {
            audits.Add(CreateAudit(task.Id, currentUserId, "Description", task.Description, dto.Description?.Trim()));
            task.Description = dto.Description?.Trim();
        }

        if (task.StatusId != dto.StatusId)
        {
            audits.Add(CreateAudit(task.Id, currentUserId, "Status", task.Status?.Name, status.Name, AuditActionType.StatusChanged));
            task.StatusId = dto.StatusId;
        }

        if (task.PriorityId != dto.PriorityId)
        {
            audits.Add(CreateAudit(task.Id, currentUserId, "Priority", task.Priority?.Name, priority.Name, AuditActionType.PriorityChanged));
            task.PriorityId = dto.PriorityId;
        }

        if (task.AssignedUserId != dto.AssignedUserId)
        {
            var oldAssignee = task.AssignedUser?.FullName ?? "Unassigned";
            string newAssignee = "Unassigned";
            if (dto.AssignedUserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(dto.AssignedUserId.Value, cancellationToken);
                newAssignee = user?.FullName ?? "Unassigned";
            }

            audits.Add(CreateAudit(task.Id, currentUserId, "AssignedUser", oldAssignee, newAssignee, 
                dto.AssignedUserId.HasValue ? AuditActionType.Assigned : AuditActionType.Unassigned));
            task.AssignedUserId = dto.AssignedUserId;
        }

        if (task.ParentTaskId != dto.ParentTaskId)
        {
            audits.Add(CreateAudit(task.Id, currentUserId, "ParentTask", task.ParentTask?.TaskNumber, dto.ParentTaskId.ToString()));
            task.ParentTaskId = dto.ParentTaskId;
        }

        if (task.EstimatedHours != dto.EstimatedHours)
        {
            audits.Add(CreateAudit(task.Id, currentUserId, "EstimatedHours", task.EstimatedHours?.ToString(), dto.EstimatedHours?.ToString()));
            task.EstimatedHours = dto.EstimatedHours;
        }

        if (task.StartedOn != dto.StartedOn)
        {
            audits.Add(CreateAudit(task.Id, currentUserId, "StartedOn", task.StartedOn?.ToString("g"), dto.StartedOn?.ToString("g")));
            task.StartedOn = dto.StartedOn;
        }

        if (task.EndOn != dto.EndOn)
        {
            audits.Add(CreateAudit(task.Id, currentUserId, "EndOn", task.EndOn?.ToString("g"), dto.EndOn?.ToString("g")));
            task.EndOn = dto.EndOn;
        }

        task.UpdatedBy = currentUserId;
        task.UpdatedOn = DateTime.UtcNow;

        _taskRepository.Update(task);

        if (audits.Count > 0)
        {
            await _taskAuditRepository.AddRangeAsync(audits, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Task {TaskId} updated successfully.", task.Id);

        // Notify new assignee if changed
        if (dto.AssignedUserId.HasValue && dto.AssignedUserId != currentUserId && audits.Any(a => a.FieldName == "AssignedUser"))
        {
            await _notificationService.CreateNotificationAsync(
                dto.AssignedUserId.Value,
                task.TenantId,
                "Task Reassigned",
                $"You have been assigned to task {task.TaskNumber}: {task.Title}",
                "TaskAssigned",
                $"/Tasks/Details/{task.Id}",
                cancellationToken);
        }

        var updated = await _taskRepository.GetByIdWithDetailsAsync(task.Id, cancellationToken);
        return Result<TaskDetailDto>.Success(MapToDetailDto(updated ?? task, currentUserId));
    }

    public async Task<Result<TaskDto>> MoveTaskAsync(MoveTaskDto dto, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdWithDetailsAsync(dto.TaskId, cancellationToken);
        if (task == null || task.IsDeleted)
        {
            return Result<TaskDto>.Failure("Task not found.");
        }

        // Validate Tenant Authorization
        if (!_tenantContext.IsGlobalAdmin && !_tenantContext.HasAccessToTenant(task.TenantId))
        {
            throw new TenantAccessDeniedException(task.TenantId, _tenantContext.TenantId);
        }

        var targetStatus = await _statusRepository.GetByIdAsync(dto.TargetStatusId, cancellationToken);
        if (targetStatus == null || !targetStatus.IsActive)
        {
            return Result<TaskDto>.Failure("Target status is invalid or inactive.");
        }

        if (task.StatusId == dto.TargetStatusId)
        {
            return Result<TaskDto>.Success(MapToDto(task));
        }

        var oldStatusName = task.Status?.Name ?? "Unknown";
        task.StatusId = dto.TargetStatusId;
        task.UpdatedBy = _currentUserService.UserId;
        task.UpdatedOn = DateTime.UtcNow;

        _taskRepository.Update(task);

        var audit = new TaskAudit
        {
            TaskId = task.Id,
            UserId = _currentUserService.UserId,
            Action = AuditActionType.MovedOnBoard.ToString(),
            FieldName = "Status",
            OldValue = oldStatusName,
            NewValue = targetStatus.Name,
            CreatedOn = DateTime.UtcNow,
            IpAddress = _currentUserService.IpAddress
        };
        await _taskAuditRepository.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Task {TaskNumber} moved from {OldStatus} to {NewStatus}.", task.TaskNumber, oldStatusName, targetStatus.Name);

        // Broadcast real-time Scrum board update across all connected clients
        await _realtimeService.SendTaskMovedAsync(
            task.TenantId,
            task.Id,
            task.TaskNumber,
            targetStatus.Id,
            oldStatusName,
            targetStatus.Name,
            _currentUserService.Username ?? "User",
            cancellationToken);

        // Notify assigned user if task was moved by someone else
        if (task.AssignedUserId.HasValue && task.AssignedUserId.Value != _currentUserService.UserId)
        {
            await _notificationService.CreateNotificationAsync(
                task.AssignedUserId.Value,
                task.TenantId,
                $"Task Status Updated: {task.TaskNumber}",
                $"{_currentUserService.FullName ?? _currentUserService.Username ?? "A team member"} moved task {task.TaskNumber} ('{task.Title}') from {oldStatusName} to {targetStatus.Name}.",
                "TaskMoved",
                $"/Tasks/Details/{task.Id}",
                cancellationToken);
        }

        var refreshed = await _taskRepository.GetByIdWithDetailsAsync(task.Id, cancellationToken);
        return Result<TaskDto>.Success(MapToDto(refreshed ?? task));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (task == null || task.IsDeleted)
        {
            return Result.Failure("Task not found.");
        }

        if (!_tenantContext.IsGlobalAdmin && !_tenantContext.HasAccessToTenant(task.TenantId))
        {
            throw new TenantAccessDeniedException(task.TenantId, _tenantContext.TenantId);
        }

        task.IsDeleted = true;
        task.DeletedOn = DateTime.UtcNow;
        task.DeletedBy = _currentUserService.UserId;

        _taskRepository.Update(task);

        var audit = new TaskAudit
        {
            TaskId = task.Id,
            UserId = _currentUserService.UserId,
            Action = AuditActionType.SoftDeleted.ToString(),
            FieldName = "Task",
            OldValue = "Active",
            NewValue = "Deleted",
            CreatedOn = DateTime.UtcNow,
            IpAddress = _currentUserService.IpAddress
        };
        await _taskAuditRepository.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Task {TaskNumber} ({TaskId}) soft deleted.", task.TaskNumber, task.Id);

        return Result.Success();
    }

    // Comments
    public async Task<Result<TaskCommentDto>> AddCommentAsync(CreateTaskCommentDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Comment))
        {
            return Result<TaskCommentDto>.Failure("Comment text cannot be empty.");
        }

        var task = await _taskRepository.GetByIdAsync(dto.TaskId, cancellationToken);
        if (task == null || task.IsDeleted)
        {
            return Result<TaskCommentDto>.Failure("Task not found.");
        }

        if (!_tenantContext.IsGlobalAdmin && !_tenantContext.HasAccessToTenant(task.TenantId))
        {
            throw new TenantAccessDeniedException(task.TenantId, _tenantContext.TenantId);
        }

        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var comment = new TaskComment
        {
            TaskId = task.Id,
            UserId = currentUserId,
            Comment = dto.Comment.Trim(),
            CreatedOn = DateTime.UtcNow
        };

        await _taskRepository.AddCommentAsync(comment, cancellationToken);

        var audit = new TaskAudit
        {
            TaskId = task.Id,
            UserId = currentUserId,
            Action = AuditActionType.CommentAdded.ToString(),
            FieldName = "Comments",
            NewValue = $"Comment added: '{dto.Comment.Trim().Substring(0, Math.Min(50, dto.Comment.Trim().Length))}...'",
            CreatedOn = DateTime.UtcNow,
            IpAddress = _currentUserService.IpAddress
        };
        await _taskAuditRepository.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify assigned user if someone else commented on their task
        if (task.AssignedUserId.HasValue && task.AssignedUserId != currentUserId)
        {
            var commentSnippet = dto.Comment.Trim();
            if (commentSnippet.Length > 60) commentSnippet = commentSnippet.Substring(0, 57) + "...";

            await _notificationService.CreateNotificationAsync(
                task.AssignedUserId.Value,
                task.TenantId,
                $"New Comment on {task.TaskNumber}",
                $"{_currentUserService.FullName ?? _currentUserService.Username ?? "A user"} commented: \"{commentSnippet}\"",
                "TaskComment",
                $"/Tasks/Details/{task.Id}",
                cancellationToken);
        }

        var commentDto = new TaskCommentDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            UserId = currentUserId,
            UserName = _currentUserService.Username ?? "User",
            UserFullName = _currentUserService.FullName ?? "User",
            Comment = comment.Comment,
            CreatedOn = comment.CreatedOn,
            CanDelete = true,
            CanEdit = true
        };

        return Result<TaskCommentDto>.Success(commentDto);
    }

    public async Task<Result> DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var comment = await _taskRepository.GetCommentByIdAsync(commentId, cancellationToken);
        if (comment == null || comment.IsDeleted)
        {
            return Result.Failure("Comment not found.");
        }

        var task = await _taskRepository.GetByIdAsync(comment.TaskId, cancellationToken);
        if (task == null)
        {
            return Result.Failure("Task not found.");
        }

        if (!_tenantContext.IsGlobalAdmin && !_tenantContext.HasAccessToTenant(task.TenantId))
        {
            throw new TenantAccessDeniedException(task.TenantId, _tenantContext.TenantId);
        }

        // Author or Admin can delete
        if (!_tenantContext.IsGlobalAdmin && comment.UserId != _currentUserService.UserId)
        {
            return Result.Failure("You can only delete your own comments.");
        }

        comment.IsDeleted = true;
        comment.DeletedOn = DateTime.UtcNow;
        comment.DeletedBy = _currentUserService.UserId;

        _taskRepository.UpdateComment(comment);

        var audit = new TaskAudit
        {
            TaskId = task.Id,
            UserId = _currentUserService.UserId,
            Action = AuditActionType.CommentDeleted.ToString(),
            FieldName = "Comments",
            OldValue = comment.Comment,
            NewValue = "Deleted",
            CreatedOn = DateTime.UtcNow,
            IpAddress = _currentUserService.IpAddress
        };
        await _taskAuditRepository.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // Attachments
    public async Task<Result<TaskAttachmentDto>> UploadAttachmentAsync(UploadAttachmentDto dto, CancellationToken cancellationToken = default)
    {
        if (!_fileStorageService.IsAllowedFileType(dto.FileName, dto.ContentType))
        {
            return Result<TaskAttachmentDto>.Failure("Invalid file type. Only PNG, JPG, JPEG, WebP images and PDF documents are allowed.");
        }

        var task = await _taskRepository.GetByIdAsync(dto.TaskId, cancellationToken);
        if (task == null || task.IsDeleted)
        {
            return Result<TaskAttachmentDto>.Failure("Task not found.");
        }

        if (!_tenantContext.IsGlobalAdmin && !_tenantContext.HasAccessToTenant(task.TenantId))
        {
            throw new TenantAccessDeniedException(task.TenantId, _tenantContext.TenantId);
        }

        var (storedFileName, storagePath, fileSize) = await _fileStorageService.SaveFileAsync(
            dto.FileStream, dto.FileName, dto.ContentType, cancellationToken);

        var currentUserId = _currentUserService.UserId ?? Guid.Empty;
        var attachment = new TaskAttachment
        {
            TaskId = task.Id,
            FileName = Path.GetFileName(dto.FileName),
            StoredFileName = storedFileName,
            ContentType = dto.ContentType,
            FileSize = fileSize,
            StoragePath = storagePath,
            UploadedBy = currentUserId,
            UploadedOn = DateTime.UtcNow
        };

        await _taskRepository.AddAttachmentAsync(attachment, cancellationToken);

        var audit = new TaskAudit
        {
            TaskId = task.Id,
            UserId = currentUserId,
            Action = AuditActionType.AttachmentAdded.ToString(),
            FieldName = "Attachments",
            NewValue = $"Uploaded '{attachment.FileName}' ({attachment.FileSize} bytes)",
            CreatedOn = DateTime.UtcNow,
            IpAddress = _currentUserService.IpAddress
        };
        await _taskAuditRepository.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var attachmentDto = new TaskAttachmentDto
        {
            Id = attachment.Id,
            TaskId = attachment.TaskId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize,
            UploadedBy = currentUserId,
            UploaderName = _currentUserService.FullName ?? "User",
            UploadedOn = attachment.UploadedOn
        };

        return Result<TaskAttachmentDto>.Success(attachmentDto);
    }

    public async Task<Result<(Stream stream, string contentType, string fileName)>> GetAttachmentFileAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await _taskRepository.GetAttachmentByIdAsync(attachmentId, cancellationToken);
        if (attachment == null || attachment.IsDeleted)
        {
            return Result<(Stream, string, string)>.Failure("Attachment not found.");
        }

        var task = await _taskRepository.GetByIdAsync(attachment.TaskId, cancellationToken);
        if (task == null)
        {
            return Result<(Stream, string, string)>.Failure("Task not found.");
        }

        if (!_tenantContext.IsGlobalAdmin && !_tenantContext.HasAccessToTenant(task.TenantId))
        {
            throw new TenantAccessDeniedException(task.TenantId, _tenantContext.TenantId);
        }

        var stream = await _fileStorageService.GetFileStreamAsync(attachment.StoragePath, cancellationToken);
        if (stream == null)
        {
            return Result<(Stream, string, string)>.Failure("File content not found on server.");
        }

        return Result<(Stream, string, string)>.Success((stream, attachment.ContentType, attachment.FileName));
    }

    public async Task<Result> DeleteAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await _taskRepository.GetAttachmentByIdAsync(attachmentId, cancellationToken);
        if (attachment == null || attachment.IsDeleted)
        {
            return Result.Failure("Attachment not found.");
        }

        var task = await _taskRepository.GetByIdAsync(attachment.TaskId, cancellationToken);
        if (task == null)
        {
            return Result.Failure("Task not found.");
        }

        if (!_tenantContext.IsGlobalAdmin && !_tenantContext.HasAccessToTenant(task.TenantId))
        {
            throw new TenantAccessDeniedException(task.TenantId, _tenantContext.TenantId);
        }

        attachment.IsDeleted = true;
        attachment.DeletedOn = DateTime.UtcNow;
        attachment.DeletedBy = _currentUserService.UserId;

        _taskRepository.UpdateAttachment(attachment);

        var audit = new TaskAudit
        {
            TaskId = task.Id,
            UserId = _currentUserService.UserId,
            Action = AuditActionType.AttachmentDeleted.ToString(),
            FieldName = "Attachments",
            OldValue = attachment.FileName,
            NewValue = "Deleted",
            CreatedOn = DateTime.UtcNow,
            IpAddress = _currentUserService.IpAddress
        };
        await _taskAuditRepository.AddAsync(audit, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private TaskAudit CreateAudit(Guid taskId, Guid? userId, string field, string? oldValue, string? newValue, AuditActionType action = AuditActionType.Updated)
    {
        return new TaskAudit
        {
            TaskId = taskId,
            UserId = userId,
            Action = action.ToString(),
            FieldName = field,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedOn = DateTime.UtcNow,
            IpAddress = _currentUserService.IpAddress
        };
    }

    private static TaskDto MapToDto(TaskItem task)
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
            ParentTaskTitle = task.ParentTask?.Title,
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
            UpdatedOn = task.UpdatedOn,
            CommentCount = task.Comments?.Count(c => !c.IsDeleted) ?? 0,
            AttachmentCount = task.Attachments?.Count(a => !a.IsDeleted) ?? 0,
            ChildTaskCount = task.ChildTasks?.Count(ct => !ct.IsDeleted) ?? 0,
            RowVersion = task.RowVersion
        };
    }

    private static TaskDetailDto MapToDetailDto(TaskItem task, Guid? currentUserId)
    {
        var baseDto = MapToDto(task);
        return new TaskDetailDto
        {
            Id = baseDto.Id,
            TaskNumber = baseDto.TaskNumber,
            TenantId = baseDto.TenantId,
            TenantName = baseDto.TenantName,
            TenantCode = baseDto.TenantCode,
            Title = baseDto.Title,
            Description = baseDto.Description,
            AssignedUserId = baseDto.AssignedUserId,
            AssignedUserName = baseDto.AssignedUserName,
            ParentTaskId = baseDto.ParentTaskId,
            ParentTaskNumber = baseDto.ParentTaskNumber,
            ParentTaskTitle = baseDto.ParentTaskTitle,
            EstimatedHours = baseDto.EstimatedHours,
            StartedOn = baseDto.StartedOn,
            EndOn = baseDto.EndOn,
            StatusId = baseDto.StatusId,
            StatusName = baseDto.StatusName,
            StatusColor = baseDto.StatusColor,
            PriorityId = baseDto.PriorityId,
            PriorityName = baseDto.PriorityName,
            PriorityColor = baseDto.PriorityColor,
            CreatedBy = baseDto.CreatedBy,
            CreatedByName = baseDto.CreatedByName,
            CreatedOn = baseDto.CreatedOn,
            UpdatedOn = baseDto.UpdatedOn,
            CommentCount = baseDto.CommentCount,
            AttachmentCount = baseDto.AttachmentCount,
            ChildTaskCount = baseDto.ChildTaskCount,
            RowVersion = baseDto.RowVersion,
            Tenant = task.Tenant == null ? null : new TenantDto
            {
                Id = task.Tenant.Id,
                Name = task.Tenant.Name,
                Code = task.Tenant.Code,
                Prefix = task.Tenant.Prefix,
                IsActive = task.Tenant.IsActive
            },
            AssignedUser = task.AssignedUser == null ? null : new UserDto
            {
                Id = task.AssignedUser.Id,
                FirstName = task.AssignedUser.FirstName,
                LastName = task.AssignedUser.LastName,
                FullName = task.AssignedUser.FullName,
                Email = task.AssignedUser.Email,
                Username = task.AssignedUser.Username
            },
            Creator = task.Creator == null ? null : new UserDto
            {
                Id = task.Creator.Id,
                FirstName = task.Creator.FirstName,
                LastName = task.Creator.LastName,
                FullName = task.Creator.FullName,
                Email = task.Creator.Email,
                Username = task.Creator.Username
            },
            Status = task.Status == null ? null : new TaskStatusDto
            {
                Id = task.Status.Id,
                Name = task.Status.Name,
                Code = task.Status.Code,
                Color = task.Status.Color,
                DisplayOrder = task.Status.DisplayOrder
            },
            Priority = task.Priority == null ? null : new TaskPriorityDto
            {
                Id = task.Priority.Id,
                Name = task.Priority.Name,
                Code = task.Priority.Code,
                Color = task.Priority.Color,
                DisplayOrder = task.Priority.DisplayOrder
            },
            ParentTask = task.ParentTask == null ? null : MapToDto(task.ParentTask),
            ChildTasks = task.ChildTasks?.Where(c => !c.IsDeleted).Select(MapToDto).ToList() ?? [],
            Comments = task.Comments?.Where(c => !c.IsDeleted).OrderBy(c => c.CreatedOn).Select(c => new TaskCommentDto
            {
                Id = c.Id,
                TaskId = c.TaskId,
                UserId = c.UserId,
                UserName = c.User?.Username ?? "User",
                UserFullName = c.User?.FullName ?? "User",
                Comment = c.Comment,
                CreatedOn = c.CreatedOn,
                UpdatedOn = c.UpdatedOn,
                CanDelete = currentUserId.HasValue && (c.UserId == currentUserId.Value || c.User?.Username == "admin"),
                CanEdit = currentUserId.HasValue && (c.UserId == currentUserId.Value)
            }).ToList() ?? [],
            Attachments = task.Attachments?.Where(a => !a.IsDeleted).OrderBy(a => a.UploadedOn).Select(a => new TaskAttachmentDto
            {
                Id = a.Id,
                TaskId = a.TaskId,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize,
                UploadedBy = a.UploadedBy,
                UploaderName = a.Uploader?.FullName ?? "User",
                UploadedOn = a.UploadedOn
            }).ToList() ?? [],
            Audits = task.Audits?.OrderByDescending(a => a.CreatedOn).Select(a => new TaskAuditDto
            {
                Id = a.Id,
                TaskId = a.TaskId,
                TaskNumber = task.TaskNumber,
                UserId = a.UserId,
                UserName = a.User?.Username ?? "System",
                UserFullName = a.User?.FullName ?? "System",
                Action = a.Action,
                FieldName = a.FieldName,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                CreatedOn = a.CreatedOn,
                IpAddress = a.IpAddress
            }).ToList() ?? []
        };
    }
}
