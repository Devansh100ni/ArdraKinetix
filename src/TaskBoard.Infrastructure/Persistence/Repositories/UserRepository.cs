using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly TaskBoardDbContext _context;

    public UserRepository(TaskBoardDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByEmailOrUsernameAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var lower = identifier.Trim().ToLower();
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserTenants)
                .ThenInclude(ut => ut.Tenant)
            .FirstOrDefaultAsync(u => (u.Email.ToLower() == lower || u.Username.ToLower() == lower) && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetWithRolesAndTenantsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserTenants)
                .ThenInclude(ut => ut.Tenant)
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<PagedResult<User>> GetPagedAsync(FilterRequest request, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserTenants)
                .ThenInclude(ut => ut.Tenant)
            .AsNoTracking()
            .Where(u => !u.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(u => u.UserTenants.Any(ut => ut.TenantId == tenantId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(u => u.FirstName.ToLower().Contains(search) 
                                  || u.LastName.ToLower().Contains(search) 
                                  || u.Email.ToLower().Contains(search) 
                                  || u.Username.ToLower().Contains(search));
        }

        query = request.SortBy?.ToLower() switch
        {
            "username" => request.SortDescending ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "email" => request.SortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "firstname" => request.SortDescending ? query.OrderByDescending(u => u.FirstName) : query.OrderBy(u => u.FirstName),
            "isactive" => request.SortDescending ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
            _ => query.OrderByDescending(u => u.CreatedOn)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<User>(items, totalCount, request.PageNumber, request.PageSize);
    }

    public async Task<IReadOnlyList<User>> GetUsersByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserTenants)
            .AsNoTracking()
            .Where(u => u.IsActive && !u.IsDeleted && u.UserTenants.Any(ut => ut.TenantId == tenantId))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToUpperInvariant();
        return await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalized, cancellationToken);
    }

    public async Task<bool> ExistsEmailAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var lower = email.Trim().ToLower();
        return await _context.Users.AnyAsync(u => u.Email.ToLower() == lower && (!excludeId.HasValue || u.Id != excludeId.Value) && !u.IsDeleted, cancellationToken);
    }

    public async Task<bool> ExistsUsernameAsync(string username, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var lower = username.Trim().ToLower();
        return await _context.Users.AnyAsync(u => u.Username.ToLower() == lower && (!excludeId.HasValue || u.Id != excludeId.Value) && !u.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        if (!exists)
        {
            await _context.UserRoles.AddAsync(new UserRole { UserId = userId, RoleId = roleId, AssignedOn = DateTime.UtcNow }, cancellationToken);
        }
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var userRole = await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
        }
    }

    public async Task AssignToTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.UserTenants.AnyAsync(ut => ut.UserId == userId && ut.TenantId == tenantId, cancellationToken);
        if (!exists)
        {
            await _context.UserTenants.AddAsync(new UserTenant { UserId = userId, TenantId = tenantId, AssignedOn = DateTime.UtcNow }, cancellationToken);
        }
    }

    public async Task RemoveFromTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var userTenant = await _context.UserTenants.FirstOrDefaultAsync(ut => ut.UserId == userId && ut.TenantId == tenantId, cancellationToken);
        if (userTenant != null)
        {
            _context.UserTenants.Remove(userTenant);
        }
    }
}
