using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Statuses;

namespace TaskBoard.Application.Features.Statuses;

public interface ITaskStatusService
{
    Task<TaskStatusDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskStatusDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskStatusDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<TaskStatusDto>> CreateAsync(CreateTaskStatusDto dto, CancellationToken cancellationToken = default);
    Task<Result<TaskStatusDto>> UpdateAsync(UpdateTaskStatusDto dto, CancellationToken cancellationToken = default);
    Task<Result> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
}
