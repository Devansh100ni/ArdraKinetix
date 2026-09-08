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
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code = null, string? correlationId = null)
    {
        var requestId = correlationId ?? Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var errorMessage = HttpContext.Items["ErrorMessage"]?.ToString() ?? "An unexpected error occurred while processing your request.";

        return View(new ErrorViewModel
        {
            RequestId = requestId,
            StatusCode = code ?? HttpContext.Response.StatusCode,
            ErrorMessage = errorMessage
        });
    }
}

