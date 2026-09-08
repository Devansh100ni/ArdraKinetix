using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Features.Dashboard;
using TaskBoard.Application.Features.Tenants;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Enums;
using TaskBoard.Web.ViewModels;

namespace TaskBoard.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContext _tenantContext;

    public DashboardController(
        IDashboardService dashboardService,
        ITenantService tenantService,
        ICurrentUserService currentUserService,
        ITenantContext tenantContext)
    {
        _dashboardService = dashboardService;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _tenantContext = tenantContext;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var role = _currentUserService.Role ?? SystemRoles.TenantUser;
        var viewModel = new DashboardViewModel { Role = role };

        if (role == SystemRoles.Admin)
        {
            viewModel.AdminDashboard = await _dashboardService.GetAdminDashboardAsync(cancellationToken);
            viewModel.AvailableTenants = await _tenantService.GetAllActiveAsync(cancellationToken);
        }
        else if (role == SystemRoles.Developer)
        {
            viewModel.DeveloperDashboard = await _dashboardService.GetDeveloperDashboardAsync(cancellationToken);
            viewModel.SelectedTenantId = _tenantContext.TenantId;
            viewModel.AvailableTenants = viewModel.DeveloperDashboard.AssignedTenants;
        }
        else
        {
            viewModel.TenantDashboard = await _dashboardService.GetTenantDashboardAsync(cancellationToken);
            viewModel.SelectedTenantId = _tenantContext.TenantId;
        }

        return View(viewModel);
    }
}
