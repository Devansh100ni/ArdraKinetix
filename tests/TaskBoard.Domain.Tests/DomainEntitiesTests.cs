using FluentAssertions;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;
using Xunit;

namespace TaskBoard.Domain.Tests;

public class DomainEntitiesTests
{
    [Fact]
    public void Tenant_Initialization_HasCorrectDefaults()
    {
        var tenant = new Tenant
        {
            Name = "Acme Corp",
            Code = "ACM",
            Prefix = "ACM",
            IsActive = true
        };

        tenant.Id.Should().NotBeEmpty();
        tenant.IsActive.Should().BeTrue();
        tenant.IsDeleted.Should().BeFalse();
        tenant.Code.Should().Be("ACM");
        tenant.Prefix.Should().Be("ACM");
    }

    [Fact]
    public void TaskItem_Hierarchy_SupportsParentAndChildTasks()
    {
        var parentTask = new TaskItem
        {
            TaskNumber = "ACM-000001",
            TenantId = Guid.NewGuid(),
            Title = "Parent Feature Epic"
        };

        var childTask = new TaskItem
        {
            TaskNumber = "ACM-000002",
            TenantId = parentTask.TenantId,
            ParentTaskId = parentTask.Id,
            Title = "Child Implementation Task"
        };

        parentTask.ChildTasks.Add(childTask);

        childTask.ParentTaskId.Should().Be(parentTask.Id);
        parentTask.ChildTasks.Should().ContainSingle();
        parentTask.ChildTasks.First().TaskNumber.Should().Be("ACM-000002");
    }

    [Fact]
    public void UserRole_SystemRoleConstants_AreCorrect()
    {
        SystemRoles.Admin.Should().Be("Admin");
        SystemRoles.Developer.Should().Be("Developer");
        SystemRoles.TenantUser.Should().Be("TenantUser");
        SystemRoles.AllRoles.Should().BeEquivalentTo(new[] { "Admin", "Developer", "TenantUser" });
    }
}
