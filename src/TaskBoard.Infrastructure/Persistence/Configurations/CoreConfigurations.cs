using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Code).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Prefix).HasMaxLength(10).IsRequired();
        builder.Property(t => t.IsActive).HasDefaultValue(true);
        builder.Property(t => t.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(t => t.Code).IsUnique();
        builder.HasIndex(t => t.Prefix).IsUnique();
        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.IsDeleted);

        builder.HasOne(t => t.TaskSequence)
            .WithOne(ts => ts.Tenant)
            .HasForeignKey<TenantTaskSequence>(ts => ts.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Username).HasMaxLength(50).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.IsDeleted).HasDefaultValue(false);
        builder.Property(u => u.IsLockedOut).HasDefaultValue(false);
        builder.Property(u => u.FailedLoginAttempts).HasDefaultValue(0);
        builder.Property(u => u.LockoutReason).HasMaxLength(250);
        builder.Property(u => u.MustChangePassword).HasDefaultValue(false);
        builder.Property(u => u.SecurityStamp).HasMaxLength(100).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => u.IsDeleted);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Token).HasMaxLength(256).IsRequired();
        builder.Property(rt => rt.CreatedByIp).HasMaxLength(50);
        builder.Property(rt => rt.RevokedByIp).HasMaxLength(50);
        builder.Property(rt => rt.ReplacedByToken).HasMaxLength(256);
        builder.Property(rt => rt.ReasonRevoked).HasMaxLength(250);

        builder.HasIndex(rt => new { rt.UserId, rt.Token });
        builder.HasIndex(rt => rt.ExpiresUtc);

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
        builder.Property(n => n.TargetUrl).HasMaxLength(500);
        builder.Property(n => n.IsRead).HasDefaultValue(false);

        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.HasIndex(n => n.CreatedOn);

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Tenant)
            .WithMany()
            .HasForeignKey(n => n.TenantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(50).IsRequired();
        builder.Property(r => r.NormalizedName).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(250);

        builder.HasIndex(r => r.NormalizedName).IsUnique();
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserTenantConfiguration : IEntityTypeConfiguration<UserTenant>
{
    public void Configure(EntityTypeBuilder<UserTenant> builder)
    {
        builder.ToTable("UserTenants");
        builder.HasKey(ut => new { ut.UserId, ut.TenantId });

        builder.HasOne(ut => ut.User)
            .WithMany(u => u.UserTenants)
            .HasForeignKey(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ut => ut.Tenant)
            .WithMany(t => t.UserTenants)
            .HasForeignKey(ut => ut.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ut => ut.TenantId);
        builder.HasIndex(ut => ut.UserId);
    }
}

public class TenantTaskSequenceConfiguration : IEntityTypeConfiguration<TenantTaskSequence>
{
    public void Configure(EntityTypeBuilder<TenantTaskSequence> builder)
    {
        builder.ToTable("TenantTaskSequences");
        builder.HasKey(ts => ts.TenantId);

        builder.Property(ts => ts.LastNumber).IsRequired().HasDefaultValue(0);
    }
}
