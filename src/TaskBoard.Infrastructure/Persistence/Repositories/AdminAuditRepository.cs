using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

public class AdminAuditRepository : IAdminAuditRepository
{
    private readonly TaskBoardDbContext _context;

    public AdminAuditRepository(TaskBoardDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AdminAudit>> GetPagedAsync(FilterRequest request, string? entityType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AdminAudits
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(a => a.Action.ToLower().Contains(search) 
                                  || a.EntityType.ToLower().Contains(search)
                                  || (a.Details != null && a.Details.ToLower().Contains(search)));
        }

        query = query.OrderByDescending(a => a.CreatedOn);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminAudit>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task AddAsync(AdminAudit audit, CancellationToken cancellationToken = default)
    {
        await _context.AdminAudits.AddAsync(audit, cancellationToken);
    }
}
