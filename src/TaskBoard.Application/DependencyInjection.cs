using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Features.Authentication;
using TaskBoard.Application.Features.Audit;
using TaskBoard.Application.Features.Dashboard;
using TaskBoard.Application.Features.Priorities;
using TaskBoard.Application.Features.ScrumBoards;
using TaskBoard.Application.Features.Statuses;
using TaskBoard.Application.Features.Tasks;
using TaskBoard.Application.Features.Tenants;
using TaskBoard.Application.Features.Users;
using TaskBoard.Application.Validators;

namespace TaskBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Feature Application Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IScrumBoardService, ScrumBoardService>();
        services.AddScoped<ITaskStatusService, TaskStatusService>();
        services.AddScoped<ITaskPriorityService, TaskPriorityService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        return services;
    }
}
