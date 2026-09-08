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
        OnChallenge = context =>
        {
            var isAjax = string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                         || (context.Request.Headers.Accept.Any(h => h != null && h.Contains("application/json")) && !context.Request.Headers.Accept.Any(h => h != null && h.Contains("text/html")));

            if (!context.Request.Path.StartsWithSegments("/api") && !isAjax)
            {
                context.HandleResponse();
                context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// For WebApplicationFactory in integration tests
public partial class Program { }
