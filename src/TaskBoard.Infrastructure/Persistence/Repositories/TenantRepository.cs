using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly TaskBoardDbContext _context;

    public TenantRepository(TaskBoardDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .Include(t => t.UserTenants)
                .ThenInclude(ut => ut.User)
            .Include(t => t.Tasks.Where(task => !task.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
    }

    public async Task<Tenant?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Code == code && !t.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Tenant>> GetPagedAsync(FilterRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Tenants
            .Include(t => t.UserTenants)
            .Include(t => t.Tasks.Where(task => !task.IsDeleted))
            .AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(search) 
                                  || t.Code.ToLower().Contains(search) 
                                  || t.Prefix.ToLower().Contains(search));
        }

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
            "code" => request.SortDescending ? query.OrderByDescending(t => t.Code) : query.OrderBy(t => t.Code),
            "prefix" => request.SortDescending ? query.OrderByDescending(t => t.Prefix) : query.OrderBy(t => t.Prefix),
            "isactive" => request.SortDescending ? query.OrderByDescending(t => t.IsActive) : query.OrderBy(t => t.IsActive),
            _ => query.OrderByDescending(t => t.CreatedOn)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Tenant>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<bool> ExistsCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AnyAsync(t => t.Code == code && (!excludeId.HasValue || t.Id != excludeId.Value) && !t.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        await _context.Tenants.AddAsync(tenant, cancellationToken);
    }

    public void Update(Tenant tenant)
    {
        _context.Tenants.Update(tenant);
    }
}
