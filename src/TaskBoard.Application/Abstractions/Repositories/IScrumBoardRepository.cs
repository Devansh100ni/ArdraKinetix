using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Abstractions.Repositories;

public interface IScrumBoardRepository
{
    Task<ScrumBoard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ScrumBoard?> GetDefaultBoardAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScrumBoard>> GetAllActiveAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task AddAsync(ScrumBoard board, CancellationToken cancellationToken = default);
    void Update(ScrumBoard board);

    // Columns
    Task<ScrumBoardColumn?> GetColumnByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScrumBoardColumn>> GetColumnsByBoardIdAsync(Guid boardId, CancellationToken cancellationToken = default);
    Task AddColumnAsync(ScrumBoardColumn column, CancellationToken cancellationToken = default);
    void UpdateColumn(ScrumBoardColumn column);
}
