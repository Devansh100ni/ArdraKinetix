using System.Security.Claims;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Enums;
using TaskBoard.Infrastructure.Tenancy;

namespace TaskBoard.Web.Middleware;

public class JwtAuthenticationMiddleware
{
    public const string CookieName = "TaskBoard_Auth_Token";
    public const string RefreshCookieName = "TaskBoard_Refresh_Token";
    private readonly RequestDelegate _next;

    public JwtAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        IJwtTokenService jwtTokenService, 
        IRefreshTokenService refreshTokenService,
        ITenantContext tenantContext,
        ITenantRepository tenantRepository,
        IUserRepository userRepository)
    {
        string? token = null;

        // 1. Try Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authHeader.Substring("Bearer ".Length).Trim();
        }

        // 2. Try Cookie if no header token
        if (string.IsNullOrWhiteSpace(token) && context.Request.Cookies.TryGetValue(CookieName, out var cookieToken))
        {
            token = cookieToken;
        }

        Guid? authenticatedUserId = null;
        string? userRole = null;
        Guid? userTenantId = null;
        IReadOnlyList<Guid> userAllowedTenantIds = [];

        if (!string.IsNullOrWhiteSpace(token))
        {
            if (jwtTokenService.ValidateToken(token, out var userId, out var role, out var tokenTenantId, out var allowedTenantIds, out var securityStamp, out var expiresUtc))
            {
                var user = await userRepository.GetByIdAsync(userId);
                if (user != null && user.IsActive && !user.IsDeleted && !user.IsLockedOut)
                {
                    // Check if SecurityStamp matches (session revocation check)
                    if (string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal))
                    {
                        authenticatedUserId = user.Id;
                        userRole = role;
                        userTenantId = tokenTenantId;
                        userAllowedTenantIds = allowedTenantIds;

                        var claims = new List<Claim>
                        {
                            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                            new(ClaimTypes.Name, user.Username),
                            new(ClaimTypes.Email, user.Email),
                            new(ClaimTypes.Role, role),
                            new("FullName", user.FullName),
                            new("SecurityStamp", user.SecurityStamp)
                        };

                        if (tokenTenantId.HasValue)
                        {
                            claims.Add(new Claim("TenantId", tokenTenantId.Value.ToString()));
                        }

                        if (allowedTenantIds.Count > 0)
                        {
                            claims.Add(new Claim("AllowedTenants", string.Join(",", allowedTenantIds)));
                        }

                        var identity = new ClaimsIdentity(claims, "JwtAuth");
                        context.User = new ClaimsPrincipal(identity);

                        // Sliding Refresh: If JWT access token has <= 5 minutes remaining, slide it if valid refresh token exists
                        if (expiresUtc.HasValue && (expiresUtc.Value - DateTime.UtcNow).TotalMinutes <= 5)
                        {
                            if (context.Request.Cookies.TryGetValue(RefreshCookieName, out var refToken))
                            {
                                var validRef = await refreshTokenService.ValidateRefreshTokenAsync(refToken);
                                if (validRef != null && validRef.UserId == user.Id)
                                {
                                    var freshToken = jwtTokenService.GenerateToken(user, role, tokenTenantId, allowedTenantIds);
                                    context.Response.Cookies.Append(CookieName, freshToken, new CookieOptions
                                    {
                                        HttpOnly = true,
                                        Secure = true,
                                        SameSite = SameSiteMode.Lax,
                                        Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        // SecurityStamp mismatch: Session revoked! Clear cookies
                        context.Response.Cookies.Delete(CookieName);
                        context.Response.Cookies.Delete(RefreshCookieName);
                    }
                }
            }
        }

        // 3. Fallback: If no valid JWT, attempt recovery via 5-day Refresh Token cookie
        if (authenticatedUserId == null && context.Request.Cookies.TryGetValue(RefreshCookieName, out var fallbackRefToken))
        {
            var validRef = await refreshTokenService.ValidateRefreshTokenAsync(fallbackRefToken);
            if (validRef != null)
            {
                var user = await userRepository.GetWithRolesAndTenantsAsync(validRef.UserId);
                if (user != null && user.IsActive && !user.IsDeleted && !user.IsLockedOut)
                {
                    var primaryRole = user.UserRoles.FirstOrDefault()?.Role.Name ?? SystemRoles.TenantUser;
                    var assignedTenants = user.UserTenants
                        .Where(ut => ut.Tenant != null && ut.Tenant.IsActive && !ut.Tenant.IsDeleted)
                        .Select(ut => ut.Tenant)
                        .ToList();
                    var allowedTenantIds = assignedTenants.Select(t => t.Id).ToList();
                    var primaryTenant = assignedTenants.FirstOrDefault();
                    Guid? primaryTenantId = primaryRole == SystemRoles.Admin ? null : primaryTenant?.Id;

                    var freshToken = jwtTokenService.GenerateToken(user, primaryRole, primaryTenantId, allowedTenantIds);
                    context.Response.Cookies.Append(CookieName, freshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                    });

                    authenticatedUserId = user.Id;
                    userRole = primaryRole;
                    userTenantId = primaryTenantId;
                    userAllowedTenantIds = allowedTenantIds;

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new(ClaimTypes.Name, user.Username),
                        new(ClaimTypes.Email, user.Email),
                        new(ClaimTypes.Role, primaryRole),
                        new("FullName", user.FullName),
                        new("SecurityStamp", user.SecurityStamp)
                    };

                    if (primaryTenantId.HasValue)
                    {
                        claims.Add(new Claim("TenantId", primaryTenantId.Value.ToString()));
                    }

                    if (allowedTenantIds.Count > 0)
                    {
                        claims.Add(new Claim("AllowedTenants", string.Join(",", allowedTenantIds)));
                    }

                    var identity = new ClaimsIdentity(claims, "JwtAuth");
                    context.User = new ClaimsPrincipal(identity);
                }
            }
        }

        // Initialize Tenant Context if authenticated
        if (authenticatedUserId.HasValue && userRole != null)
        {
            bool isGlobalAdmin = userRole == SystemRoles.Admin;
            Guid? activeTenantId = userTenantId;
            string? tenantCode = null;
            string? tenantPrefix = null;

            // Allow developer to switch active tenant via cookie
            if (userRole == SystemRoles.Developer)
            {
                if (context.Request.Cookies.TryGetValue("TaskBoard_Active_Tenant", out var activeCookie) &&
                    Guid.TryParse(activeCookie, out var switchedTenantId) &&
                    userAllowedTenantIds.Contains(switchedTenantId))
                {
                    activeTenantId = switchedTenantId;
                }
                else if (activeTenantId == null && userAllowedTenantIds.Count > 0)
                {
                    activeTenantId = userAllowedTenantIds[0];
                }
            }

            if (activeTenantId.HasValue)
            {
                var tenant = await tenantRepository.GetByIdAsync(activeTenantId.Value);
                if (tenant != null)
                {
                    tenantCode = tenant.Code;
                    tenantPrefix = tenant.Prefix;
                }
            }

            if (tenantContext is TenantContext concreteContext)
            {
                concreteContext.Initialize(activeTenantId, tenantCode, tenantPrefix, isGlobalAdmin, userAllowedTenantIds);
            }
        }

        await _next(context);
    }
}
