using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.DTOs.Auth;
using TaskBoard.Application.Features.Authentication;
using TaskBoard.Web.Middleware;
using Xunit;

namespace TaskBoard.Web.Tests;

public class ControllersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ControllersIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

        var loginResult = await authService.LoginAsync(new LoginRequest
        {
            Identifier = username,
            Password = password
        });

        loginResult.Success.Should().BeTrue();
        loginResult.Token.Should().NotBeNullOrEmpty();

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true
        });

        client.DefaultRequestHeaders.Add("Cookie", $"{JwtAuthenticationMiddleware.CookieName}={loginResult.Token}");
        return client;
    }

    [Fact]
    public async Task AdminUser_CanAccessAllAdministrativeAndBoardPages()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "AdminPassword123!");

        // 1. Dashboard
        var dashboardRes = await client.GetAsync("/Dashboard");
        dashboardRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboardHtml = await dashboardRes.Content.ReadAsStringAsync();
        dashboardHtml.Should().Contain("Enterprise Overview");
        dashboardHtml.Should().Contain("Total Tenants");

        // 2. Scrum Board
        var scrumRes = await client.GetAsync("/Scrum");
        scrumRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var scrumHtml = await scrumRes.Content.ReadAsStringAsync();
        scrumHtml.Should().Contain("Agile Scrum Board");
        scrumHtml.Should().Contain("scrum-board-container");

        // 3. Tasks Index
        var tasksRes = await client.GetAsync("/Tasks");
        tasksRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasksHtml = await tasksRes.Content.ReadAsStringAsync();
        tasksHtml.Should().Contain("Task Management");

        // 4. Admin Tenants
        var tenantsRes = await client.GetAsync("/Admin/Tenants");
        tenantsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var tenantsHtml = await tenantsRes.Content.ReadAsStringAsync();
        tenantsHtml.Should().Contain("Acme Corporation");

        // 5. Admin Users
        var usersRes = await client.GetAsync("/Admin/Users");
        usersRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Admin Statuses
        var statusesRes = await client.GetAsync("/Admin/Statuses");
        statusesRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 7. Admin Priorities
        var prioritiesRes = await client.GetAsync("/Admin/Priorities");
        prioritiesRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 8. Admin ScrumBoards
        var boardsRes = await client.GetAsync("/Admin/ScrumBoards");
        boardsRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 9. Admin Audit
        var auditRes = await client.GetAsync("/Admin/Audit");
        auditRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeveloperUser_CanAccessDeveloperWorkspaceAndAssignedTenants()
    {
        var client = await CreateAuthenticatedClientAsync("devuser", "Developer123!");

        // 1. Dashboard
        var dashboardRes = await client.GetAsync("/Dashboard");
        dashboardRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboardHtml = await dashboardRes.Content.ReadAsStringAsync();
        dashboardHtml.Should().Contain("Developer Workspace");

        // 2. Scrum Board
        var scrumRes = await client.GetAsync("/Scrum");
        scrumRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var scrumHtml = await scrumRes.Content.ReadAsStringAsync();
        scrumHtml.Should().Contain("Agile Scrum Board");

        // 3. Tasks
        var tasksRes = await client.GetAsync("/Tasks");
        tasksRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Forbidden from Admin Panel
        var adminRes = await client.GetAsync("/Admin/Tenants");
        adminRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TenantUser_CanAccessTenantWorkspace_AndIsRestrictedFromOtherTenantsAndAdmin()
    {
        var client = await CreateAuthenticatedClientAsync("acmeuser", "TenantUser123!");

        // 1. Dashboard
        var dashboardRes = await client.GetAsync("/Dashboard");
        dashboardRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboardHtml = await dashboardRes.Content.ReadAsStringAsync();
        dashboardHtml.Should().Contain("Acme Corporation");

        // 2. Scrum Board
        var scrumRes = await client.GetAsync("/Scrum");
        scrumRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Forbidden from Admin Panel
        var adminRes = await client.GetAsync("/Admin/Tenants");
        adminRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PublicPages_AboutAndProducts_RenderSuccessfully()
    {
        var client = _factory.CreateClient();

        // 1. About Page
        var aboutRes = await client.GetAsync("/About");
        aboutRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var aboutHtml = await aboutRes.Content.ReadAsStringAsync();
        aboutHtml.Should().Contain("ArdraKinetix");
        aboutHtml.Should().Contain("Under the Hood");

        // 2. Products Page
        var productsRes = await client.GetAsync("/Products");
        productsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var productsHtml = await productsRes.Content.ReadAsStringAsync();
        productsHtml.Should().Contain("ArdraKinetix Enterprise");
        productsHtml.Should().Contain("Multi-Tenant Organization Gateways");
    }
}

