using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

public class ScrumBoardRepository : IScrumBoardRepository
{
    private readonly TaskBoardDbContext _context;

    public ScrumBoardRepository(TaskBoardDbContext context)
    {
        _context = context;
    }

    public async Task<ScrumBoard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ScrumBoards
            .Include(b => b.Columns.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Status)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken);
    }

    public async Task<ScrumBoard?> GetDefaultBoardAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        // First try tenant-specific board
        if (tenantId.HasValue)
        {
            var tenantBoard = await _context.ScrumBoards
                .Include(b => b.Columns.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.Status)
                .FirstOrDefaultAsync(b => b.TenantId == tenantId.Value && b.IsActive && !b.IsDeleted, cancellationToken);

            if (tenantBoard != null)
            {
                return tenantBoard;
            }
        }

        // Fall back to global default board (TenantId is null)
        return await _context.ScrumBoards
            .Include(b => b.Columns.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Status)
            .FirstOrDefaultAsync(b => b.TenantId == null && b.IsActive && !b.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<ScrumBoard>> GetAllActiveAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        return await _context.ScrumBoards
            .Include(b => b.Columns.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Status)
            .AsNoTracking()
            .Where(b => (b.TenantId == null || (tenantId.HasValue && b.TenantId == tenantId.Value)) && b.IsActive && !b.IsDeleted)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ScrumBoard board, CancellationToken cancellationToken = default)
    {
        await _context.ScrumBoards.AddAsync(board, cancellationToken);
    }

    public void Update(ScrumBoard board)
    {
        _context.ScrumBoards.Update(board);
    }

    public async Task<ScrumBoardColumn?> GetColumnByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ScrumBoardColumns
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<ScrumBoardColumn>> GetColumnsByBoardIdAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        return await _context.ScrumBoardColumns
            .Include(c => c.Status)
            .AsNoTracking()
            .Where(c => c.BoardId == boardId && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task AddColumnAsync(ScrumBoardColumn column, CancellationToken cancellationToken = default)
    {
        await _context.ScrumBoardColumns.AddAsync(column, cancellationToken);
    }

    public void UpdateColumn(ScrumBoardColumn column)
    {
        _context.ScrumBoardColumns.Update(column);
    }
}
