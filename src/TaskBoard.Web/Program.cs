using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Application;
using TaskBoard.Domain.Enums;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Migrations;
using TaskBoard.Web.Authorization;
using TaskBoard.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add layers
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add MVC Controllers with Views
builder.Services.AddControllersWithViews(options =>
{
    // Auto validate antiforgery tokens on POST/PUT/DELETE
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// Configure JWT Authentication
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "TaskBoard_Super_Secret_Production_Key_2026_MinLength32Chars!";
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtOrCookie";
    options.DefaultChallengeScheme = "JwtOrCookie";
})
.AddPolicyScheme("JwtOrCookie", "JWT or Cookie Scheme", options =>
{
    options.ForwardDefaultSelector = context => JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TaskBoardEnterpriseApp",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TaskBoardEnterpriseClients",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // 1. SignalR Hub WebSockets & Long Polling send token in query string "access_token"
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
                return Task.CompletedTask;
            }

            // 2. Extract Authorization header if present
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = authHeader.Substring("Bearer ".Length).Trim();
                return Task.CompletedTask;
            }

            // 3. Fallback to auth cookie
            if (context.Request.Cookies.TryGetValue(JwtAuthenticationMiddleware.CookieName, out var cookieToken))
            {
                context.Token = cookieToken;
            }

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var isHub = context.Request.Path.StartsWithSegments("/hubs");
            var isApi = context.Request.Path.StartsWithSegments("/api");
            var isAjax = string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                         || (context.Request.Headers.Accept.Any(h => h != null && h.Contains("application/json")) && !context.Request.Headers.Accept.Any(h => h != null && h.Contains("text/html")));

            if (!isHub && !isApi && !isAjax)
            {
                context.HandleResponse();
                var rawReturn = context.Request.Path + context.Request.QueryString;
                var encodedReturn = Uri.EscapeDataString(rawReturn);
                context.Response.Redirect($"/Account/Login?returnUrl={encodedReturn}");
            }
            return Task.CompletedTask;
        }
    };
});

// Policy-based Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.RequireAdmin, policy => policy.RequireRole(SystemRoles.Admin));
    options.AddPolicy(PolicyNames.RequireDeveloperOrAdmin, policy => policy.RequireRole(SystemRoles.Admin, SystemRoles.Developer));
    options.AddPolicy(PolicyNames.RequireAuthenticated, policy => policy.RequireAuthenticatedUser());
});

var app = builder.Build();

// Run Evolve Database Migrations on Startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
        migrator.MigrateDatabase();
        logger.LogInformation("Database migration initialized successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed on application startup.");
    }
}

// Status Code Error Page Re-execute (Catches 404 Not Found, 403 Forbidden, 500 etc.)
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

// Exception Handling Middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// JWT Cookie / Header extraction and Tenant Context Initialization
app.UseMiddleware<JwtAuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<TaskBoard.Infrastructure.Realtime.TaskBoardHub>("/hubs/taskboard");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// For WebApplicationFactory in integration tests
public partial class Program { }
