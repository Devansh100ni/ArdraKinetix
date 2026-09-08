using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

public class TaskPriorityRepository : ITaskPriorityRepository
{
    private readonly TaskBoardDbContext _context;

    public TaskPriorityRepository(TaskBoardDbContext context)
    {
        _context = context;
    }

    public async Task<TaskPriority?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TaskPriorities
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
    }

    public async Task<TaskPriority?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var upper = code.Trim().ToUpperInvariant();
        return await _context.TaskPriorities
            .FirstOrDefaultAsync(p => p.Code == upper && !p.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskPriority>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaskPriorities
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskPriority>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaskPriorities
            .Include(p => p.Tasks.Where(t => !t.IsDeleted))
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var upper = code.Trim().ToUpperInvariant();
        return await _context.TaskPriorities
            .AnyAsync(p => p.Code == upper && (!excludeId.HasValue || p.Id != excludeId.Value) && !p.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(TaskPriority priority, CancellationToken cancellationToken = default)
    {
        await _context.TaskPriorities.AddAsync(priority, cancellationToken);
    }

    public void Update(TaskPriority priority)
    {
        _context.TaskPriorities.Update(priority);
    }
}
