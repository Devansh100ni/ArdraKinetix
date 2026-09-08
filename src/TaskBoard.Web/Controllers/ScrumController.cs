using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Features.ScrumBoards;
using TaskBoard.Application.Features.Tenants;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Enums;
using TaskBoard.Web.ViewModels;

namespace TaskBoard.Web.Controllers;

[Authorize]
public class ScrumController : Controller
{
    private readonly IScrumBoardService _scrumBoardService;
    private readonly ITenantService _tenantService;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public ScrumController(
        IScrumBoardService scrumBoardService,
        ITenantService tenantService,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService)
    {
        _scrumBoardService = scrumBoardService;
        _tenantService = tenantService;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var effectiveTenantId = tenantId ?? _tenantContext.TenantId;

        // Security check for non-admins
        if (!_tenantContext.IsGlobalAdmin && effectiveTenantId.HasValue && !_tenantContext.HasAccessToTenant(effectiveTenantId.Value))
        {
            effectiveTenantId = _tenantContext.TenantId;
        }

        var board = await _scrumBoardService.GetActiveBoardAsync(effectiveTenantId, cancellationToken);

        var availableTenants = _tenantContext.IsGlobalAdmin
            ? await _tenantService.GetAllActiveAsync(cancellationToken)
            : _currentUserService.IsInRole(SystemRoles.Developer)
                ? (await _tenantService.GetAllActiveAsync(cancellationToken)).Where(t => _tenantContext.AllowedTenantIds.Contains(t.Id)).ToList()
                : [];

        string? selectedTenantName = null;
        if (effectiveTenantId.HasValue)
        {
            var t = await _tenantService.GetByIdAsync(effectiveTenantId.Value, cancellationToken);
            selectedTenantName = t?.Name;
        }

        var viewModel = new ScrumBoardViewModel
        {
            Board = board,
            AvailableTenants = availableTenants,
            SelectedTenantId = effectiveTenantId,
            SelectedTenantName = selectedTenantName,
            CanCreateTask = true
        };

        return View(viewModel);
    }
}
