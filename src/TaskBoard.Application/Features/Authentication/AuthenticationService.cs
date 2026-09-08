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
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
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

        // Check if user is currently locked out
        if (user.IsLockedOut)
        {
            if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
            {
                var remainingMinutes = (int)Math.Ceiling((user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes);
                _logger.LogWarning("Login attempt for locked out user: {UserId}. Remaining: {RemainingMinutes} mins", user.Id, remainingMinutes);
                return new LoginResponse
                {
                    Success = false,
                    IsLockedOut = true,
                    LockoutMinutesRemaining = remainingMinutes,
                    Error = $"Account is locked due to multiple failed login attempts ({user.LockoutReason ?? "Security policy"}). Please try again in {remainingMinutes} minute(s) or contact an administrator."
                };
            }
            else if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value <= DateTime.UtcNow)
            {
                // Lockout expired, automatically unlock
                user.IsLockedOut = false;
                user.LockoutEndUtc = null;
                user.FailedLoginAttempts = 0;
                user.LockoutReason = null;
            }
            else
            {
                // Permanent administrative lock
                _logger.LogWarning("Login attempt for administratively locked user: {UserId}", user.Id);
                return new LoginResponse
                {
                    Success = false,
                    IsLockedOut = true,
                    Error = $"Account is locked by an administrator ({user.LockoutReason ?? "Administrative lock"}). Please contact support."
                };
            }
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(user, user.PasswordHash, request.Password);
        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.IsLockedOut = true;
                user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(30);
                user.LockoutReason = "5 consecutive invalid password attempts";
                _logger.LogWarning("User {UserId} locked out for 30 mins after {Attempts} failed attempts.", user.Id, user.FailedLoginAttempts);

                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new LoginResponse
                {
                    Success = false,
                    IsLockedOut = true,
                    LockoutMinutesRemaining = 30,
                    Error = "Account has been locked for 30 minutes due to 5 consecutive failed password attempts. Please try again later or contact your administrator."
                };
            }

            _userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            int remainingAttempts = 5 - user.FailedLoginAttempts;
            _logger.LogWarning("Invalid password for user: {UserId}. Attempt {Attempts} of 5", user.Id, user.FailedLoginAttempts);
            return new LoginResponse
            {
                Success = false,
                Error = $"Invalid email/username or password. ({remainingAttempts} attempt(s) remaining before account lockout)."
            };
        }

        // Successful password verification: Reset failed attempts & unlock
        user.FailedLoginAttempts = 0;
        user.IsLockedOut = false;
        user.LockoutEndUtc = null;
        user.LockoutReason = null;

        // Ensure security stamp exists
        if (string.IsNullOrWhiteSpace(user.SecurityStamp))
        {
            user.SecurityStamp = Guid.NewGuid().ToString();
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

        // Generate 30-Minute JWT Token
        var token = _jwtTokenService.GenerateToken(user, primaryRole, primaryTenantId, allowedTenantIds);

        // Generate 5-Day Refresh Token
        var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id, _currentUserService.IpAddress, cancellationToken);

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
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresUtc = refreshToken.ExpiresUtc,
            MustChangePassword = user.MustChangePassword,
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
