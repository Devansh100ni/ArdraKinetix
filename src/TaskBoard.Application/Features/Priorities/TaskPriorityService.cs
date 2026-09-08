using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Priorities;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Priorities;

public class TaskPriorityService : ITaskPriorityService
{
    private readonly ITaskPriorityRepository _priorityRepository;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TaskPriorityService> _logger;

    public TaskPriorityService(
        ITaskPriorityRepository priorityRepository,
        IAdminAuditRepository adminAuditRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<TaskPriorityService> logger)
    {
        _priorityRepository = priorityRepository;
        _adminAuditRepository = adminAuditRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TaskPriorityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var priority = await _priorityRepository.GetByIdAsync(id, cancellationToken);
        return priority == null ? null : MapToDto(priority);
    }

    public async Task<IReadOnlyList<TaskPriorityDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var priorities = await _priorityRepository.GetAllActiveAsync(cancellationToken);
        return priorities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<TaskPriorityDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var priorities = await _priorityRepository.GetAllAsync(cancellationToken);
        return priorities.Select(MapToDto).ToList();
    }

    public async Task<Result<TaskPriorityDto>> CreateAsync(CreateTaskPriorityDto dto, CancellationToken cancellationToken = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _priorityRepository.ExistsCodeAsync(code, null, cancellationToken))
        {
            return Result<TaskPriorityDto>.Failure($"Priority code '{code}' already exists.");
        }

        var priority = new TaskPriority
        {
            Name = dto.Name.Trim(),
            Code = code,
            DisplayOrder = dto.DisplayOrder,
            Color = string.IsNullOrWhiteSpace(dto.Color) ? "#64748b" : dto.Color.Trim(),
            IsActive = dto.IsActive
        };

        await _priorityRepository.AddAsync(priority, cancellationToken);

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = "TaskPriorityCreated",
            EntityType = "TaskPriority",
            EntityId = priority.Id.ToString(),
            Details = $"Created priority '{priority.Name}' ({priority.Code})",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TaskPriorityDto>.Success(MapToDto(priority));
    }

    public async Task<Result<TaskPriorityDto>> UpdateAsync(UpdateTaskPriorityDto dto, CancellationToken cancellationToken = default)
    {
        var priority = await _priorityRepository.GetByIdAsync(dto.Id, cancellationToken);
        if (priority == null)
        {
            return Result<TaskPriorityDto>.Failure("Priority not found.");
        }

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _priorityRepository.ExistsCodeAsync(code, dto.Id, cancellationToken))
        {
            return Result<TaskPriorityDto>.Failure($"Priority code '{code}' already exists.");
        }

        priority.Name = dto.Name.Trim();
        priority.Code = code;
        priority.DisplayOrder = dto.DisplayOrder;
        priority.Color = string.IsNullOrWhiteSpace(dto.Color) ? "#64748b" : dto.Color.Trim();
        priority.IsActive = dto.IsActive;

        _priorityRepository.Update(priority);

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = "TaskPriorityUpdated",
            EntityType = "TaskPriority",
            EntityId = priority.Id.ToString(),
            Details = $"Updated priority '{priority.Name}' ({priority.Code})",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TaskPriorityDto>.Success(MapToDto(priority));
    }

    public async Task<Result> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var priority = await _priorityRepository.GetByIdAsync(id, cancellationToken);
        if (priority == null)
        {
            return Result.Failure("Priority not found.");
        }

        priority.IsActive = !priority.IsActive;
        _priorityRepository.Update(priority);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static TaskPriorityDto MapToDto(TaskPriority priority)
    {
        return new TaskPriorityDto
        {
            Id = priority.Id,
            Name = priority.Name,
            Code = priority.Code,
            DisplayOrder = priority.DisplayOrder,
            Color = priority.Color,
            IsActive = priority.IsActive,
            TaskCount = priority.Tasks?.Count(t => !t.IsDeleted) ?? 0
        };
    }
}
