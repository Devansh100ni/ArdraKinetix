using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Priorities;

namespace TaskBoard.Application.Features.Priorities;

public interface ITaskPriorityService
{
    Task<TaskPriorityDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskPriorityDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskPriorityDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<TaskPriorityDto>> CreateAsync(CreateTaskPriorityDto dto, CancellationToken cancellationToken = default);
    Task<Result<TaskPriorityDto>> UpdateAsync(UpdateTaskPriorityDto dto, CancellationToken cancellationToken = default);
    Task<Result> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
}
