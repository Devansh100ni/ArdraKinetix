using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Repositories;

public interface ITaskPriorityRepository
{
    Task<TaskPriority?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskPriority?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskPriority>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskPriority>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(TaskPriority priority, CancellationToken cancellationToken = default);
    void Update(TaskPriority priority);
}
