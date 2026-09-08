using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

public class TaskStatusRepository : ITaskStatusRepository
{
    private readonly TaskBoardDbContext _context;

    public TaskStatusRepository(TaskBoardDbContext context)
    {
        _context = context;
    }

    public async Task<TaskStatusItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TaskStatuses
            .Include(s => s.Tasks.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
    }

    public async Task<TaskStatusItem?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var upper = code.Trim().ToUpperInvariant();
        return await _context.TaskStatuses
            .FirstOrDefaultAsync(s => s.Code == upper && !s.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskStatusItem>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaskStatuses
            .Include(s => s.Tasks.Where(t => !t.IsDeleted))
            .AsNoTracking()
            .Where(s => s.IsActive && !s.IsDeleted)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskStatusItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaskStatuses
            .Include(s => s.Tasks.Where(t => !t.IsDeleted))
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var upper = code.Trim().ToUpperInvariant();
        return await _context.TaskStatuses
            .AnyAsync(s => s.Code == upper && (!excludeId.HasValue || s.Id != excludeId.Value) && !s.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(TaskStatusItem status, CancellationToken cancellationToken = default)
    {
        await _context.TaskStatuses.AddAsync(status, cancellationToken);
    }

    public void Update(TaskStatusItem status)
    {
        _context.TaskStatuses.Update(status);
    }
}
