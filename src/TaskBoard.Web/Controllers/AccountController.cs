using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.DTOs.Auth;
using TaskBoard.Application.Features.Authentication;
using TaskBoard.Web.Middleware;
using TaskBoard.Web.ViewModels;

namespace TaskBoard.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAuthenticationService authenticationService, ILogger<AccountController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _authenticationService.LoginAsync(new LoginRequest
        {
            Identifier = model.Identifier,
            Password = model.Password,
            RememberMe = model.RememberMe
        }, cancellationToken);

        if (!response.Success || string.IsNullOrEmpty(response.Token))
        {
            model.ErrorMessage = response.Error ?? "Invalid username/email or password.";
            return View(model);
        }

        // Set secure HttpOnly cookie for web session
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(2)
        };

        Response.Cookies.Append(JwtAuthenticationMiddleware.CookieName, response.Token, cookieOptions);

        _logger.LogInformation("User {Username} successfully logged in.", model.Identifier);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(JwtAuthenticationMiddleware.CookieName);
        Response.Cookies.Delete("TaskBoard_Active_Tenant");
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied(string? requestedPath = null)
    {
        return View(new AccessDeniedViewModel
        {
            RequestedPath = requestedPath ?? Request.Path,
            Message = "You do not have permission to access this resource or tenant."
        });
    }
}
