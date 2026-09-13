using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Web.ViewModels;

namespace TaskBoard.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    [HttpGet("About")]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet("Products")]
    public IActionResult Products()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code = null, string? correlationId = null)
    {
        var statusCode = code ?? HttpContext.Response.StatusCode;
        if (statusCode == 200) statusCode = 404; // default for unknown routes hit directly

        var requestId = correlationId ?? Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var customMessage = HttpContext.Items["ErrorMessage"]?.ToString();

        var message = customMessage ?? statusCode switch
        {
            404 => "The requested page, task, or workspace resource could not be found. It may have been moved, deleted, or the URL might be invalid.",
            403 => "Access is forbidden. You do not have the required permissions or tenant scope to access this page.",
            401 => "Authentication is required to view this workspace resource.",
            500 => "An unexpected server error occurred while processing your request. Our telemetry has logged the incident.",
            _ => "An unexpected condition occurred while processing your request."
        };

        return View(new ErrorViewModel
        {
            RequestId = requestId,
            StatusCode = statusCode,
            ErrorMessage = message
        });
    }
}

