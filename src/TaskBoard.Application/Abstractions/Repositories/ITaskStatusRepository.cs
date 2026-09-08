using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Repositories;

public interface ITaskStatusRepository
{
    Task<TaskStatusItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskStatusItem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskStatusItem>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskStatusItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(TaskStatusItem status, CancellationToken cancellationToken = default);
    void Update(TaskStatusItem status);
}
