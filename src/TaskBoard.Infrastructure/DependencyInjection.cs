using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Security;
using TaskBoard.Infrastructure.Authentication;
using TaskBoard.Infrastructure.FileStorage;
using TaskBoard.Infrastructure.Migrations;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Persistence.Repositories;
using TaskBoard.Infrastructure.Tenancy;

namespace TaskBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=TaskBoardDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        // EF Core DbContext
        services.AddDbContext<TaskBoardDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(TaskBoardDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            }));

        // Repositories & UnitOfWork
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IScrumBoardRepository, ScrumBoardRepository>();
        services.AddScoped<ITaskStatusRepository, TaskStatusRepository>();
        services.AddScoped<ITaskPriorityRepository, TaskPriorityRepository>();
        services.AddScoped<ITaskAuditRepository, TaskAuditRepository>();
        services.AddScoped<IAdminAuditRepository, AdminAuditRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Security & Tenancy Services
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ITaskNumberGenerator, TaskNumberGenerator>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Evolve Migrator
        services.AddTransient<DatabaseMigrator>();

        return services;
    }
}
