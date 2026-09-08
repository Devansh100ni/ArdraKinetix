using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.DTOs.Auth;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Features.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResponse { Success = false, Error = "Username/Email and Password are required." };
        }

        var user = await _userRepository.GetByEmailOrUsernameAsync(request.Identifier.Trim(), cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for identifier: {Identifier}", request.Identifier);
            return new LoginResponse { Success = false, Error = "Invalid email/username or password." };
        }

        if (!user.IsActive || user.IsDeleted)
        {
            _logger.LogWarning("Login attempt for deactivated user: {UserId}", user.Id);
            return new LoginResponse { Success = false, Error = "This account is inactive. Please contact your system administrator." };
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(user, user.PasswordHash, request.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Invalid password for user: {UserId}", user.Id);
            return new LoginResponse { Success = false, Error = "Invalid email/username or password." };
        }

        // Determine user role (defaults to TenantUser if no role assigned)
        var primaryRole = user.UserRoles.FirstOrDefault()?.Role.Name ?? SystemRoles.TenantUser;

        // Determine tenant memberships
        var assignedTenants = user.UserTenants
            .Where(ut => ut.Tenant != null && ut.Tenant.IsActive && !ut.Tenant.IsDeleted)
            .Select(ut => ut.Tenant)
            .ToList();

        var allowedTenantIds = assignedTenants.Select(t => t.Id).ToList();
        var primaryTenant = assignedTenants.FirstOrDefault();
        Guid? primaryTenantId = primaryRole == SystemRoles.Admin ? null : primaryTenant?.Id;

        // Update LastLoginOn
        user.LastLoginOn = DateTime.UtcNow;
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate JWT Token
        var token = _jwtTokenService.GenerateToken(user, primaryRole, primaryTenantId, allowedTenantIds);

        var currentUser = new CurrentUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Role = primaryRole,
            TenantId = primaryTenantId,
            TenantCode = primaryTenant?.Code,
            TenantName = primaryTenant?.Name,
            AllowedTenantIds = allowedTenantIds,
            AllowedTenants = assignedTenants.Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                Prefix = t.Prefix,
                IsActive = t.IsActive,
                CreatedOn = t.CreatedOn
            }).ToList()
        };

        _logger.LogInformation("User {UserId} ({Role}) logged in successfully.", user.Id, primaryRole);

        return new LoginResponse
        {
            Success = true,
            Token = token,
            User = currentUser
        };
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return null;
        }

        var user = await _userRepository.GetWithRolesAndTenantsAsync(_currentUserService.UserId.Value, cancellationToken);
        if (user == null || !user.IsActive || user.IsDeleted)
        {
            return null;
        }

        var primaryRole = user.UserRoles.FirstOrDefault()?.Role.Name ?? SystemRoles.TenantUser;
        var assignedTenants = user.UserTenants
            .Where(ut => ut.Tenant != null && ut.Tenant.IsActive && !ut.Tenant.IsDeleted)
            .Select(ut => ut.Tenant)
            .ToList();

        var allowedTenantIds = assignedTenants.Select(t => t.Id).ToList();
        var primaryTenant = assignedTenants.FirstOrDefault();
        Guid? primaryTenantId = primaryRole == SystemRoles.Admin ? null : primaryTenant?.Id;

        return new CurrentUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Role = primaryRole,
            TenantId = primaryTenantId,
            TenantCode = primaryTenant?.Code,
            TenantName = primaryTenant?.Name,
            AllowedTenantIds = allowedTenantIds,
            AllowedTenants = assignedTenants.Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Code = t.Code,
                Prefix = t.Prefix,
                IsActive = t.IsActive,
                CreatedOn = t.CreatedOn
            }).ToList()
        };
    }
}
