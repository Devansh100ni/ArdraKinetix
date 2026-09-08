using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.Features.Tasks;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Exceptions;
using TaskBoard.Infrastructure.Tenancy;
using Xunit;

namespace TaskBoard.Web.Tests;

public class SecurityAndTenancyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityAndTenancyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnauthenticatedUser_AccessingProtectedTasks_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Contain("/Account/Login");
    }

    [Fact]
    public async Task CrossTenantAccess_TenantUserAccessingOtherTenantTask_ThrowsForbiddenOrAccessDenied()
    {
        using var scope = _factory.Services.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        var acmeTenantId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var betaTenantId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        var betaTaskId = Guid.Parse("70000000-0000-0000-0000-000000000001");

        // Simulate Acme Tenant User context (restricted strictly to Acme)
        if (tenantContext is TenantContext concreteCtx)
        {
            concreteCtx.Initialize(acmeTenantId, "ACM", "ACM", isGlobalAdmin: false, allowedTenantIds: [acmeTenantId]);
        }

        // Attempting to retrieve a Beta Dynamics task from Acme context
        var act = async () => await taskService.GetByIdAsync(betaTaskId);

        // Must reject cross-tenant access with TenantAccessDeniedException (IDOR Protection)
        await act.Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task DeveloperTenantAccess_CanAccessAssignedTenants_Only()
    {
        using var scope = _factory.Services.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();

        var acmeTenantId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        var betaTenantId = Guid.Parse("40000000-0000-0000-0000-000000000002");
        var acmeTaskId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var betaTaskId = Guid.Parse("70000000-0000-0000-0000-000000000001");

        // Simulate Developer assigned to both Acme and Beta
        if (tenantContext is TenantContext concreteCtx)
        {
            concreteCtx.Initialize(acmeTenantId, "ACM", "ACM", isGlobalAdmin: false, allowedTenantIds: [acmeTenantId, betaTenantId]);
        }

        // Developer can access tasks in both Acme and Beta
        var acmeTask = await taskService.GetByIdAsync(acmeTaskId);
        acmeTask.Should().NotBeNull();
        acmeTask!.TaskNumber.Should().Be("ACM-000001");

        var betaTask = await taskService.GetByIdAsync(betaTaskId);
        betaTask.Should().NotBeNull();
        betaTask!.TaskNumber.Should().Be("BET-000001");
    }
}
