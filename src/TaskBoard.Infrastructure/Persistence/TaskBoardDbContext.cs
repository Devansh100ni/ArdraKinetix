using Microsoft.EntityFrameworkCore;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence;

public class TaskBoardDbContext : DbContext
{
    public TaskBoardDbContext(DbContextOptions<TaskBoardDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    public DbSet<TenantTaskSequence> TenantTaskSequences => Set<TenantTaskSequence>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskStatusItem> TaskStatuses => Set<TaskStatusItem>();
    public DbSet<TaskPriority> TaskPriorities => Set<TaskPriority>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
    public DbSet<TaskAudit> TaskAudits => Set<TaskAudit>();
    public DbSet<AdminAudit> AdminAudits => Set<AdminAudit>();
    public DbSet<ScrumBoard> ScrumBoards => Set<ScrumBoard>();
    public DbSet<ScrumBoardColumn> ScrumBoardColumns => Set<ScrumBoardColumn>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskBoardDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedOn == default)
                {
                    entry.Entity.CreatedOn = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedOn = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
