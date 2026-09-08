using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.DTOs.Auth;
using TaskBoard.Application.Features.Authentication;
using TaskBoard.Application.Features.Users;
using TaskBoard.Application.Security;
using TaskBoard.Web.Middleware;
using TaskBoard.Web.ViewModels;

namespace TaskBoard.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserService _userService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthenticationService authenticationService,
        IUserService userService,
        IRefreshTokenService refreshTokenService,
        ICurrentUserService currentUserService,
        ILogger<AccountController> logger)
    {
        _authenticationService = authenticationService;
        _userService = userService;
        _refreshTokenService = refreshTokenService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null, string? reason = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var model = new LoginViewModel { ReturnUrl = returnUrl };
        if (reason == "session_revoked")
        {
            model.ErrorMessage = "Your session was terminated or revoked. Please sign in again.";
        }
        else if (reason == "locked")
        {
            model.ErrorMessage = "Your account has been locked. Please contact your system administrator.";
        }

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
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

        // 1. Set 30-Minute JWT Access Token Cookie
        var jwtCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(30)
        };
        Response.Cookies.Append(JwtAuthenticationMiddleware.CookieName, response.Token, jwtCookieOptions);

        // 2. Set 5-Day Persistent Refresh Token Cookie
        if (!string.IsNullOrEmpty(response.RefreshToken))
        {
            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = response.RefreshTokenExpiresUtc ?? DateTimeOffset.UtcNow.AddDays(5)
            };
            Response.Cookies.Append(JwtAuthenticationMiddleware.RefreshCookieName, response.RefreshToken, refreshCookieOptions);
        }

        _logger.LogInformation("User {Username} successfully logged in.", model.Identifier);

        // Check if user is required to reset password
        if (response.MustChangePassword)
        {
            TempData["InfoMessage"] = "An administrator has required you to reset your password before continuing.";
            return RedirectToAction(nameof(ChangePassword));
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.NewPassword != model.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(model.ConfirmPassword), "New password and confirmation password do not match.");
            return View(model);
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await _userService.ChangePasswordAsync(_currentUserService.UserId.Value, model.CurrentPassword, model.NewPassword, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to change password.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Your password has been changed successfully. Please log in with your new password.";
        
        // Clean session cookies so user signs in cleanly
        Response.Cookies.Delete(JwtAuthenticationMiddleware.CookieName);
        Response.Cookies.Delete(JwtAuthenticationMiddleware.RefreshCookieName);
        Response.Cookies.Delete("TaskBoard_Active_Tenant");

        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(JwtAuthenticationMiddleware.RefreshCookieName, out var refreshToken))
        {
            await _refreshTokenService.RevokeRefreshTokenAsync(refreshToken, _currentUserService.IpAddress, "User logged out");
        }

        Response.Cookies.Delete(JwtAuthenticationMiddleware.CookieName);
        Response.Cookies.Delete(JwtAuthenticationMiddleware.RefreshCookieName);
        Response.Cookies.Delete("TaskBoard_Active_Tenant");

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied(string? requestedPath = null)
    {
        return View(new AccessDeniedViewModel
        {
            RequestedPath = requestedPath ?? Request.Path,
            Message = "You do not have permission to access this resource or tenant."
        });
    }
}
