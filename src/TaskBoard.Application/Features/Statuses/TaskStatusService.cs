using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Statuses;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Statuses;

public class TaskStatusService : ITaskStatusService
{
    private readonly ITaskStatusRepository _statusRepository;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TaskStatusService> _logger;

    public TaskStatusService(
        ITaskStatusRepository statusRepository,
        IAdminAuditRepository adminAuditRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<TaskStatusService> logger)
    {
        _statusRepository = statusRepository;
        _adminAuditRepository = adminAuditRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TaskStatusDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var status = await _statusRepository.GetByIdAsync(id, cancellationToken);
        return status == null ? null : MapToDto(status);
    }

    public async Task<IReadOnlyList<TaskStatusDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _statusRepository.GetAllActiveAsync(cancellationToken);
        return statuses.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<TaskStatusDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _statusRepository.GetAllAsync(cancellationToken);
        return statuses.Select(MapToDto).ToList();
    }

    public async Task<Result<TaskStatusDto>> CreateAsync(CreateTaskStatusDto dto, CancellationToken cancellationToken = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _statusRepository.ExistsCodeAsync(code, null, cancellationToken))
        {
            return Result<TaskStatusDto>.Failure($"Status code '{code}' already exists.");
        }

        var status = new TaskStatusItem
        {
            Name = dto.Name.Trim(),
            Code = code,
            Description = dto.Description?.Trim(),
            DisplayOrder = dto.DisplayOrder,
            Color = string.IsNullOrWhiteSpace(dto.Color) ? "#64748b" : dto.Color.Trim(),
            IsActive = dto.IsActive,
            CreatedOn = DateTime.UtcNow
        };

        await _statusRepository.AddAsync(status, cancellationToken);

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = "TaskStatusCreated",
            EntityType = "TaskStatus",
            EntityId = status.Id.ToString(),
            Details = $"Created status '{status.Name}' ({status.Code})",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Task status {StatusId} created.", status.Id);

        return Result<TaskStatusDto>.Success(MapToDto(status));
    }

    public async Task<Result<TaskStatusDto>> UpdateAsync(UpdateTaskStatusDto dto, CancellationToken cancellationToken = default)
    {
        var status = await _statusRepository.GetByIdAsync(dto.Id, cancellationToken);
        if (status == null)
        {
            return Result<TaskStatusDto>.Failure("Status not found.");
        }

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _statusRepository.ExistsCodeAsync(code, dto.Id, cancellationToken))
        {
            return Result<TaskStatusDto>.Failure($"Status code '{code}' already exists.");
        }

        status.Name = dto.Name.Trim();
        status.Code = code;
        status.Description = dto.Description?.Trim();
        status.DisplayOrder = dto.DisplayOrder;
        status.Color = string.IsNullOrWhiteSpace(dto.Color) ? "#64748b" : dto.Color.Trim();
        status.IsActive = dto.IsActive;
        status.UpdatedOn = DateTime.UtcNow;

        _statusRepository.Update(status);

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = "TaskStatusUpdated",
            EntityType = "TaskStatus",
            EntityId = status.Id.ToString(),
            Details = $"Updated status '{status.Name}' ({status.Code})",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<TaskStatusDto>.Success(MapToDto(status));
    }

    public async Task<Result> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var status = await _statusRepository.GetByIdAsync(id, cancellationToken);
        if (status == null)
        {
            return Result.Failure("Status not found.");
        }

        status.IsActive = !status.IsActive;
        status.UpdatedOn = DateTime.UtcNow;

        _statusRepository.Update(status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static TaskStatusDto MapToDto(TaskStatusItem status)
    {
        return new TaskStatusDto
        {
            Id = status.Id,
            Name = status.Name,
            Code = status.Code,
            Description = status.Description,
            DisplayOrder = status.DisplayOrder,
            Color = status.Color,
            IsActive = status.IsActive,
            TaskCount = status.Tasks?.Count(t => !t.IsDeleted) ?? 0
        };
    }
}
