using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Scrum;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.ScrumBoards;

public class ScrumBoardService : IScrumBoardService
{
    private readonly IScrumBoardRepository _scrumBoardRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskStatusRepository _statusRepository;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScrumBoardService> _logger;

    public ScrumBoardService(
        IScrumBoardRepository scrumBoardRepository,
        ITaskRepository taskRepository,
        ITaskStatusRepository statusRepository,
        IAdminAuditRepository adminAuditRepository,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<ScrumBoardService> logger)
    {
        _scrumBoardRepository = scrumBoardRepository;
        _taskRepository = taskRepository;
        _statusRepository = statusRepository;
        _adminAuditRepository = adminAuditRepository;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ScrumBoardDto?> GetActiveBoardAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        // Enforce tenant context
        Guid? effectiveTenantId = tenantId;
        IReadOnlyList<Guid>? allowedTenantIds = null;

        if (!_tenantContext.IsGlobalAdmin)
        {
            if (_currentUserService.IsInRole(SystemRoles.TenantUser))
            {
                effectiveTenantId = _tenantContext.TenantId;
            }
            else if (_currentUserService.IsInRole(SystemRoles.Developer))
            {
                if (tenantId.HasValue && _tenantContext.HasAccessToTenant(tenantId.Value))
                {
                    effectiveTenantId = tenantId.Value;
                }
                else
                {
                    effectiveTenantId = _tenantContext.TenantId;
                    allowedTenantIds = _tenantContext.AllowedTenantIds;
                }
            }
        }

        var board = await _scrumBoardRepository.GetDefaultBoardAsync(effectiveTenantId, cancellationToken);
        if (board == null)
        {
            return null;
        }

        // Fetch all active board tasks for the current tenant scope
        var tasks = await _taskRepository.GetBoardTasksAsync(effectiveTenantId, allowedTenantIds, cancellationToken);

        // Group tasks by StatusId
        var tasksByStatus = tasks.GroupBy(t => t.StatusId)
            .ToDictionary(g => g.Key, g => g.Select(MapToTaskDto).ToList());

        var columnDtos = board.Columns
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c =>
            {
                var colTasks = tasksByStatus.TryGetValue(c.StatusId, out var list) ? list : [];
                return new ScrumBoardColumnDto
                {
                    Id = c.Id,
                    BoardId = c.BoardId,
                    Name = c.Name,
                    Description = c.Description,
                    DisplayOrder = c.DisplayOrder,
                    StatusId = c.StatusId,
                    StatusName = c.Status?.Name ?? "",
                    StatusCode = c.Status?.Code ?? "",
                    StatusColor = c.Status?.Color ?? "#64748b",
                    WipLimit = c.WipLimit,
                    IsActive = c.IsActive,
                    Tasks = colTasks
                };
            }).ToList();

        return new ScrumBoardDto
        {
            Id = board.Id,
            TenantId = board.TenantId,
            Name = board.Name,
            Description = board.Description,
            IsActive = board.IsActive,
            Columns = columnDtos
        };
    }

    public async Task<IReadOnlyList<ScrumBoardDto>> GetAllBoardsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var boards = await _scrumBoardRepository.GetAllActiveAsync(tenantId, cancellationToken);
        return boards.Select(b => new ScrumBoardDto
        {
            Id = b.Id,
            TenantId = b.TenantId,
            Name = b.Name,
            Description = b.Description,
            IsActive = b.IsActive,
            Columns = b.Columns.Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).Select(c => new ScrumBoardColumnDto
            {
                Id = c.Id,
                BoardId = c.BoardId,
                Name = c.Name,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                StatusId = c.StatusId,
                StatusName = c.Status?.Name ?? "",
                StatusCode = c.Status?.Code ?? "",
                StatusColor = c.Status?.Color ?? "#64748b",
                WipLimit = c.WipLimit,
                IsActive = c.IsActive
            }).ToList()
        }).ToList();
    }

    public async Task<Result<ScrumBoardDto>> CreateBoardAsync(CreateScrumBoardDto dto, CancellationToken cancellationToken = default)
    {
        var board = new ScrumBoard
        {
            TenantId = dto.TenantId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive,
            CreatedOn = DateTime.UtcNow
        };

        if (dto.Columns != null && dto.Columns.Count > 0)
        {
            foreach (var col in dto.Columns)
            {
                board.Columns.Add(new ScrumBoardColumn
                {
                    Name = col.Name.Trim(),
                    Description = col.Description?.Trim(),
                    DisplayOrder = col.DisplayOrder,
                    StatusId = col.StatusId,
                    WipLimit = col.WipLimit,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow
                });
            }
        }

        await _scrumBoardRepository.AddAsync(board, cancellationToken);
        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = "ScrumBoardCreated",
            EntityType = "ScrumBoard",
            EntityId = board.Id.ToString(),
            Details = $"Created Scrum Board '{board.Name}'",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Scrum Board {BoardId} created.", board.Id);

        return Result<ScrumBoardDto>.Success(new ScrumBoardDto
        {
            Id = board.Id,
            TenantId = board.TenantId,
            Name = board.Name,
            Description = board.Description,
            IsActive = board.IsActive
        });
    }

    public async Task<Result<ScrumBoardColumnDto>> AddColumnAsync(CreateScrumColumnDto dto, Guid boardId, CancellationToken cancellationToken = default)
    {
        var board = await _scrumBoardRepository.GetByIdAsync(boardId, cancellationToken);
        if (board == null)
        {
            return Result<ScrumBoardColumnDto>.Failure("Board not found.");
        }

        var status = await _statusRepository.GetByIdAsync(dto.StatusId, cancellationToken);
        if (status == null || !status.IsActive)
        {
            return Result<ScrumBoardColumnDto>.Failure("Invalid status specified for column.");
        }

        var column = new ScrumBoardColumn
        {
            BoardId = boardId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            DisplayOrder = dto.DisplayOrder,
            StatusId = dto.StatusId,
            WipLimit = dto.WipLimit,
            IsActive = true,
            CreatedOn = DateTime.UtcNow
        };

        await _scrumBoardRepository.AddColumnAsync(column, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ScrumBoardColumnDto>.Success(new ScrumBoardColumnDto
        {
            Id = column.Id,
            BoardId = column.BoardId,
            Name = column.Name,
            Description = column.Description,
            DisplayOrder = column.DisplayOrder,
            StatusId = column.StatusId,
            StatusName = status.Name,
            StatusCode = status.Code,
            StatusColor = status.Color,
            WipLimit = column.WipLimit,
            IsActive = column.IsActive
        });
    }

    public async Task<Result<ScrumBoardColumnDto>> UpdateColumnAsync(UpdateScrumColumnDto dto, CancellationToken cancellationToken = default)
    {
        var column = await _scrumBoardRepository.GetColumnByIdAsync(dto.Id, cancellationToken);
        if (column == null)
        {
            return Result<ScrumBoardColumnDto>.Failure("Column not found.");
        }

        var status = await _statusRepository.GetByIdAsync(dto.StatusId, cancellationToken);
        if (status == null || !status.IsActive)
        {
            return Result<ScrumBoardColumnDto>.Failure("Invalid status specified for column.");
        }

        column.Name = dto.Name.Trim();
        column.Description = dto.Description?.Trim();
        column.DisplayOrder = dto.DisplayOrder;
        column.StatusId = dto.StatusId;
        column.WipLimit = dto.WipLimit;
        column.IsActive = dto.IsActive;
        column.UpdatedOn = DateTime.UtcNow;

        _scrumBoardRepository.UpdateColumn(column);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ScrumBoardColumnDto>.Success(new ScrumBoardColumnDto
        {
            Id = column.Id,
            BoardId = column.BoardId,
            Name = column.Name,
            Description = column.Description,
            DisplayOrder = column.DisplayOrder,
            StatusId = column.StatusId,
            StatusName = status.Name,
            StatusCode = status.Code,
            StatusColor = status.Color,
            WipLimit = column.WipLimit,
            IsActive = column.IsActive
        });
    }

    public async Task<Result> DeleteColumnAsync(Guid columnId, CancellationToken cancellationToken = default)
    {
        var column = await _scrumBoardRepository.GetColumnByIdAsync(columnId, cancellationToken);
        if (column == null)
        {
            return Result.Failure("Column not found.");
        }

        column.IsDeleted = true;
        column.DeletedOn = DateTime.UtcNow;
        column.DeletedBy = _currentUserService.UserId;

        _scrumBoardRepository.UpdateColumn(column);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
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
            ChildTaskCount = task.ChildTasks?.Count(c => !c.IsDeleted) ?? 0,
            RowVersion = task.RowVersion
        };
    }
}
