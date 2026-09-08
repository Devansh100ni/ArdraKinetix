using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Security;

namespace TaskBoard.Web.Controllers;

[Authorize]
public class TenantSwitcherController : Controller
{
    private readonly ITenantContext _tenantContext;

    public TenantSwitcherController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Switch(Guid tenantId, string? returnUrl = null)
    {
        if (_tenantContext.IsGlobalAdmin || _tenantContext.AllowedTenantIds.Contains(tenantId))
        {
            Response.Cookies.Append("TaskBoard_Active_Tenant", tenantId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
