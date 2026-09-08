using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        IPasswordHasherService passwordHasher,
        IAdminAuditRepository adminAuditRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _passwordHasher = passwordHasher;
        _adminAuditRepository = adminAuditRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithRolesAndTenantsAsync(id, cancellationToken);
        return user == null ? null : MapToDto(user);
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(FilterRequest request, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var pagedUsers = await _userRepository.GetPagedAsync(request, tenantId, cancellationToken);
        var dtos = pagedUsers.Items.Select(MapToDto).ToList();
        return new PagedResult<UserDto>(dtos, pagedUsers.TotalCount, pagedUsers.PageNumber, pagedUsers.PageSize);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetUsersByTenantAsync(tenantId, cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _userRepository.GetAllRolesAsync(cancellationToken);
        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description
        }).ToList();
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var username = dto.Username.Trim();

        if (await _userRepository.ExistsEmailAsync(email, null, cancellationToken))
        {
            return Result<UserDto>.Failure($"A user with email '{email}' already exists.");
        }

        if (await _userRepository.ExistsUsernameAsync(username, null, cancellationToken))
        {
            return Result<UserDto>.Failure($"A user with username '{username}' already exists.");
        }

        var roleName = string.IsNullOrWhiteSpace(dto.Role) ? SystemRoles.TenantUser : dto.Role;
        var role = await _userRepository.GetRoleByNameAsync(roleName, cancellationToken);
        if (role == null)
        {
            return Result<UserDto>.Failure($"Invalid role specified: '{roleName}'");
        }

        // Validate tenant assignment constraints
        if (roleName == SystemRoles.TenantUser && dto.AssignedTenantIds.Count != 1)
        {
            return Result<UserDto>.Failure("Tenant users must be assigned to exactly one tenant.");
        }

        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            Username = username,
            IsActive = dto.IsActive,
            CreatedOn = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.AssignRoleAsync(user.Id, role.Id, cancellationToken);

        // Assign tenants
        foreach (var tId in dto.AssignedTenantIds)
        {
            await _userRepository.AssignToTenantAsync(user.Id, tId, cancellationToken);
        }

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = "UserCreated",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Details = $"Created user '{user.Username}' ({user.Email}) with role '{role.Name}'",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} created successfully with role {Role}.", user.Id, role.Name);

        var createdUser = await _userRepository.GetWithRolesAndTenantsAsync(user.Id, cancellationToken);
        return Result<UserDto>.Success(MapToDto(createdUser ?? user));
    }

    public async Task<Result<UserDto>> UpdateAsync(UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithRolesAndTenantsAsync(dto.Id, cancellationToken);
        if (user == null)
        {
            return Result<UserDto>.Failure("User not found.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        var username = dto.Username.Trim();

        if (await _userRepository.ExistsEmailAsync(email, dto.Id, cancellationToken))
        {
            return Result<UserDto>.Failure($"A user with email '{email}' already exists.");
        }

        if (await _userRepository.ExistsUsernameAsync(username, dto.Id, cancellationToken))
        {
            return Result<UserDto>.Failure($"A user with username '{username}' already exists.");
        }

        var roleName = string.IsNullOrWhiteSpace(dto.Role) ? SystemRoles.TenantUser : dto.Role;
        var role = await _userRepository.GetRoleByNameAsync(roleName, cancellationToken);
        if (role == null)
        {
            return Result<UserDto>.Failure($"Invalid role specified: '{roleName}'");
        }

        if (roleName == SystemRoles.TenantUser && dto.AssignedTenantIds.Count != 1)
        {
            return Result<UserDto>.Failure("Tenant users must be assigned to exactly one tenant.");
        }

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.Email = email;
        user.Username = username;
        user.IsActive = dto.IsActive;
        user.UpdatedOn = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        }

        _userRepository.Update(user);

        // Update role
        var currentRole = user.UserRoles.FirstOrDefault();
        if (currentRole == null || currentRole.RoleId != role.Id)
        {
            if (currentRole != null)
            {
                await _userRepository.RemoveRoleAsync(user.Id, currentRole.RoleId, cancellationToken);
            }
            await _userRepository.AssignRoleAsync(user.Id, role.Id, cancellationToken);
        }

        // Update tenant assignments
        var currentTenantIds = user.UserTenants.Select(ut => ut.TenantId).ToList();
        var toRemove = currentTenantIds.Except(dto.AssignedTenantIds).ToList();
        var toAdd = dto.AssignedTenantIds.Except(currentTenantIds).ToList();

        foreach (var tId in toRemove)
        {
            await _userRepository.RemoveFromTenantAsync(user.Id, tId, cancellationToken);
        }

        foreach (var tId in toAdd)
        {
            await _userRepository.AssignToTenantAsync(user.Id, tId, cancellationToken);
        }

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = "UserUpdated",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Details = $"Updated user '{user.Username}' ({user.Email}), Role: {role.Name}, Active: {user.IsActive}",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} updated successfully.", user.Id);

        var updatedUser = await _userRepository.GetWithRolesAndTenantsAsync(user.Id, cancellationToken);
        return Result<UserDto>.Success(MapToDto(updatedUser ?? user));
    }

    public async Task<Result> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        user.IsActive = !user.IsActive;
        user.UpdatedOn = DateTime.UtcNow;
        _userRepository.Update(user);

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = user.IsActive ? "UserActivated" : "UserDeactivated",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Details = $"User '{user.Username}' status changed to Active={user.IsActive}",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> AssignTenantsAsync(AssignUserTenantsDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithRolesAndTenantsAsync(dto.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        var currentTenantIds = user.UserTenants.Select(ut => ut.TenantId).ToList();
        var toRemove = currentTenantIds.Except(dto.TenantIds).ToList();
        var toAdd = dto.TenantIds.Except(currentTenantIds).ToList();

        foreach (var tId in toRemove)
        {
            await _userRepository.RemoveFromTenantAsync(user.Id, tId, cancellationToken);
        }

        foreach (var tId in toAdd)
        {
            await _userRepository.AssignToTenantAsync(user.Id, tId, cancellationToken);
        }

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = "UserTenantsAssigned",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            Details = $"Assigned {dto.TenantIds.Count} tenants to user '{user.Username}'",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Email = user.Email,
            Username = user.Username,
            IsActive = user.IsActive,
            CreatedOn = user.CreatedOn,
            LastLoginOn = user.LastLoginOn,
            Roles = user.UserRoles?.Where(ur => ur.Role != null).Select(ur => ur.Role.Name).ToList() ?? [],
            AssignedTenants = user.UserTenants?.Where(ut => ut.Tenant != null).Select(ut => new TenantDto
            {
                Id = ut.Tenant.Id,
                Name = ut.Tenant.Name,
                Code = ut.Tenant.Code,
                Prefix = ut.Tenant.Prefix,
                IsActive = ut.Tenant.IsActive,
                CreatedOn = ut.Tenant.CreatedOn
            }).ToList() ?? []
        };
    }
}
