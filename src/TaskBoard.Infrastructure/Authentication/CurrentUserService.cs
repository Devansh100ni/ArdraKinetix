using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskBoard.Application.Security;

namespace TaskBoard.Infrastructure.Authentication;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? Username => User?.FindFirst(ClaimTypes.Name)?.Value;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? FullName => User?.FindFirst("FullName")?.Value ?? Username;

    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }
}
