using System.Net;
using System.Text.Json;
using TaskBoard.Domain.Exceptions;

namespace TaskBoard.Web.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        _logger.LogError(exception, "Unhandled exception occurred. TraceIdentifier: {CorrelationId}", correlationId);

        var isAjax = string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                     || context.Request.Headers.Accept.Any(h => h != null && h.Contains("application/json"));

        var statusCode = exception switch
        {
            TenantAccessDeniedException => HttpStatusCode.Forbidden,
            NotFoundException => HttpStatusCode.NotFound,
            DomainValidationException => HttpStatusCode.BadRequest,
            ConcurrencyConflictException => HttpStatusCode.Conflict,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            _ => HttpStatusCode.InternalServerError
        };

        var message = exception switch
        {
            TenantAccessDeniedException => "Access denied. You do not have permission to access resources in this tenant.",
            NotFoundException notFound => notFound.Message,
            DomainValidationException val => val.Message,
            ConcurrencyConflictException conflict => conflict.Message,
            _ => "An unexpected server error occurred. Please try again or contact support."
        };

        context.Response.StatusCode = (int)statusCode;

        if (isAjax)
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                success = false,
                error = message,
                correlationId
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        else
        {
            if (statusCode == HttpStatusCode.Forbidden || statusCode == HttpStatusCode.Unauthorized)
            {
                var reqPath = context.Request.Path + context.Request.QueryString;
                var encodedPath = Uri.EscapeDataString(reqPath);
                context.Response.Redirect($"/Account/AccessDenied?requestedPath={encodedPath}");
            }
            else
            {
                context.Items["ErrorMessage"] = message;
                context.Items["StatusCode"] = (int)statusCode;
                context.Items["CorrelationId"] = correlationId;
                var encodedCorrelation = Uri.EscapeDataString(correlationId);
                context.Response.Redirect($"/Home/Error?code={(int)statusCode}&correlationId={encodedCorrelation}");
            }
        }
    }
}
