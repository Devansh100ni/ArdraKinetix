using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TaskNumber).HasMaxLength(30).IsRequired();
        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(4000);
        builder.Property(t => t.EstimatedHours).HasPrecision(6, 2);
        builder.Property(t => t.IsDeleted).HasDefaultValue(false);

        // Optimistic Concurrency RowVersion
        builder.Property(t => t.RowVersion).IsRowVersion();

        // Unique TaskNumber per Tenant
        builder.HasIndex(t => new { t.TenantId, t.TaskNumber }).IsUnique();

        // Indexes for fast querying & filtering
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.AssignedUserId);
        builder.HasIndex(t => t.StatusId);
        builder.HasIndex(t => t.PriorityId);
        builder.HasIndex(t => t.ParentTaskId);
        builder.HasIndex(t => t.CreatedOn);
        builder.HasIndex(t => t.StartedOn);
        builder.HasIndex(t => t.EndOn);
        builder.HasIndex(t => t.IsDeleted);

        // Relationships
        builder.HasOne(t => t.Tenant)
            .WithMany(ten => ten.Tasks)
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedUser)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Creator)
            .WithMany()
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ParentTask)
            .WithMany(p => p.ChildTasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Status)
            .WithMany(s => s.Tasks)
            .HasForeignKey(t => t.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Priority)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.PriorityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TaskStatusItemConfiguration : IEntityTypeConfiguration<TaskStatusItem>
{
    public void Configure(EntityTypeBuilder<TaskStatusItem> builder)
    {
        builder.ToTable("TaskStatuses");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Code).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(250);
        builder.Property(s => s.Color).HasMaxLength(20).HasDefaultValue("#64748b");
        builder.Property(s => s.IsActive).HasDefaultValue(true);
        builder.Property(s => s.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.DisplayOrder);
        builder.HasIndex(s => s.IsActive);
    }
}

public class TaskPriorityConfiguration : IEntityTypeConfiguration<TaskPriority>
{
    public void Configure(EntityTypeBuilder<TaskPriority> builder)
    {
        builder.ToTable("TaskPriorities");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Color).HasMaxLength(20).HasDefaultValue("#64748b");
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.DisplayOrder);
    }
}

public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("TaskComments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Comment).HasMaxLength(4000).IsRequired();
        builder.Property(c => c.IsDeleted).HasDefaultValue(false);

        builder.HasOne(c => c.Task)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.TaskId);
        builder.HasIndex(c => c.CreatedOn);
    }
}

public class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> builder)
    {
        builder.ToTable("TaskAttachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.StoredFileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(a => a.IsDeleted).HasDefaultValue(false);

        builder.HasOne(a => a.Task)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Uploader)
            .WithMany(u => u.Attachments)
            .HasForeignKey(a => a.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.TaskId);
    }
}

public class TaskAuditConfiguration : IEntityTypeConfiguration<TaskAudit>
{
    public void Configure(EntityTypeBuilder<TaskAudit> builder)
    {
        builder.ToTable("TaskAudits");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.FieldName).HasMaxLength(100);
        builder.Property(a => a.OldValue).HasMaxLength(4000);
        builder.Property(a => a.NewValue).HasMaxLength(4000);
        builder.Property(a => a.IpAddress).HasMaxLength(50);

        builder.HasOne(a => a.Task)
            .WithMany(t => t.Audits)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany(u => u.Audits)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.TaskId);
        builder.HasIndex(a => a.CreatedOn);
    }
}

public class AdminAuditConfiguration : IEntityTypeConfiguration<AdminAudit>
{
    public void Configure(EntityTypeBuilder<AdminAudit> builder)
    {
        builder.ToTable("AdminAudits");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.Details).HasMaxLength(4000);
        builder.Property(a => a.IpAddress).HasMaxLength(50);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.CreatedOn);
    }
}

public class ScrumBoardConfiguration : IEntityTypeConfiguration<ScrumBoard>
{
    public void Configure(EntityTypeBuilder<ScrumBoard> builder)
    {
        builder.ToTable("ScrumBoards");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).HasMaxLength(150).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(500);
        builder.Property(b => b.IsActive).HasDefaultValue(true);
        builder.Property(b => b.IsDeleted).HasDefaultValue(false);

        builder.HasOne(b => b.Tenant)
            .WithMany(t => t.ScrumBoards)
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.TenantId);
    }
}

public class ScrumBoardColumnConfiguration : IEntityTypeConfiguration<ScrumBoardColumn>
{
    public void Configure(EntityTypeBuilder<ScrumBoardColumn> builder)
    {
        builder.ToTable("ScrumBoardColumns");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(250);
        builder.Property(c => c.IsActive).HasDefaultValue(true);
        builder.Property(c => c.IsDeleted).HasDefaultValue(false);

        builder.HasOne(c => c.Board)
            .WithMany(b => b.Columns)
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Status)
            .WithMany(s => s.BoardColumns)
            .HasForeignKey(c => c.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.BoardId);
        builder.HasIndex(c => c.StatusId);
        builder.HasIndex(c => c.DisplayOrder);
    }
}
