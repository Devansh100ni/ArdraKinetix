using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

public class TaskAuditRepository : ITaskAuditRepository
{
    private readonly TaskBoardDbContext _context;

    public TaskAuditRepository(TaskBoardDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TaskAudit>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await _context.TaskAudits
            .Include(a => a.User)
            .AsNoTracking()
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.CreatedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<TaskAudit>> GetPagedAsync(Guid? taskId, FilterRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.TaskAudits
            .Include(a => a.User)
            .Include(a => a.Task)
            .AsNoTracking()
            .AsQueryable();

        if (taskId.HasValue)
        {
            query = query.Where(a => a.TaskId == taskId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(a => a.Action.ToLower().Contains(search) 
                                  || (a.FieldName != null && a.FieldName.ToLower().Contains(search))
                                  || (a.NewValue != null && a.NewValue.ToLower().Contains(search)));
        }

        query = query.OrderByDescending(a => a.CreatedOn);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskAudit>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task AddAsync(TaskAudit audit, CancellationToken cancellationToken = default)
    {
        await _context.TaskAudits.AddAsync(audit, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<TaskAudit> audits, CancellationToken cancellationToken = default)
    {
        await _context.TaskAudits.AddRangeAsync(audits, cancellationToken);
    }
}
