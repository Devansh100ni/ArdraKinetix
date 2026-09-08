using System.Security.Claims;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Enums;
using TaskBoard.Infrastructure.Tenancy;

namespace TaskBoard.Web.Middleware;

public class JwtAuthenticationMiddleware
{
    public const string CookieName = "TaskBoard_Auth_Token";
    private readonly RequestDelegate _next;

    public JwtAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        IJwtTokenService jwtTokenService, 
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

        if (!string.IsNullOrWhiteSpace(token))
        {
            if (jwtTokenService.ValidateToken(token, out var userId, out var role, out var tokenTenantId, out var allowedTenantIds))
            {
                var user = await userRepository.GetByIdAsync(userId);
                if (user != null && user.IsActive && !user.IsDeleted)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new(ClaimTypes.Name, user.Username),
                        new(ClaimTypes.Email, user.Email),
                        new(ClaimTypes.Role, role),
                        new("FullName", user.FullName)
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

                    // Initialize Tenant Context
                    bool isGlobalAdmin = role == SystemRoles.Admin;
                    Guid? activeTenantId = tokenTenantId;
                    string? tenantCode = null;
                    string? tenantPrefix = null;

                    // Allow developer to switch active tenant via cookie/query
                    if (role == SystemRoles.Developer)
                    {
                        if (context.Request.Cookies.TryGetValue("TaskBoard_Active_Tenant", out var activeCookie) &&
                            Guid.TryParse(activeCookie, out var switchedTenantId) &&
                            allowedTenantIds.Contains(switchedTenantId))
                        {
                            activeTenantId = switchedTenantId;
                        }
                        else if (activeTenantId == null && allowedTenantIds.Count > 0)
                        {
                            activeTenantId = allowedTenantIds[0];
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
                        concreteContext.Initialize(activeTenantId, tenantCode, tenantPrefix, isGlobalAdmin, allowedTenantIds);
                    }
                }
            }
        }

        await _next(context);
    }
}
