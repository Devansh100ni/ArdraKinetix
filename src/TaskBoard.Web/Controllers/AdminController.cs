using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Priorities;
using TaskBoard.Application.DTOs.Scrum;
using TaskBoard.Application.DTOs.Statuses;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Application.Features.Audit;
using TaskBoard.Application.Features.Priorities;
using TaskBoard.Application.Features.ScrumBoards;
using TaskBoard.Application.Features.Statuses;
using TaskBoard.Application.Features.Tenants;
using TaskBoard.Application.Features.Users;
using TaskBoard.Domain.Enums;
using TaskBoard.Web.ViewModels;

namespace TaskBoard.Web.Controllers;

[Authorize(Roles = SystemRoles.Admin)]
public class AdminController : Controller
{
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;
    private readonly ITaskStatusService _statusService;
    private readonly ITaskPriorityService _priorityService;
    private readonly IScrumBoardService _scrumBoardService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ITenantService tenantService,
        IUserService userService,
        ITaskStatusService statusService,
        ITaskPriorityService priorityService,
        IScrumBoardService scrumBoardService,
        IAuditService auditService,
        ILogger<AdminController> logger)
    {
        _tenantService = tenantService;
        _userService = userService;
        _statusService = statusService;
        _priorityService = priorityService;
        _scrumBoardService = scrumBoardService;
        _auditService = auditService;
        _logger = logger;
    }

    // ==========================================
    // TENANTS MANAGEMENT
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Tenants([FromQuery] FilterRequest filter, CancellationToken cancellationToken)
    {
        var pagedTenants = await _tenantService.GetPagedAsync(filter, cancellationToken);
        var viewModel = new AdminTenantsViewModel
        {
            Tenants = pagedTenants,
            Filter = filter
        };
        return View(viewModel);
    }

    [HttpGet]
    public IActionResult TenantsCreate()
    {
        return View(new CreateTenantDto { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TenantsCreate(CreateTenantDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _tenantService.CreateAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create tenant.");
            return View(dto);
        }

        TempData["SuccessMessage"] = $"Tenant '{dto.Name}' was created successfully.";
        return RedirectToAction(nameof(Tenants));
    }

    [HttpGet]
    public async Task<IActionResult> TenantsEdit(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByIdAsync(id, cancellationToken);
        if (tenant == null)
        {
            return NotFound();
        }

        return View(new UpdateTenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            Prefix = tenant.Prefix,
            IsActive = tenant.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TenantsEdit(UpdateTenantDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _tenantService.UpdateAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update tenant.");
            return View(dto);
        }

        TempData["SuccessMessage"] = $"Tenant '{dto.Name}' was updated successfully.";
        return RedirectToAction(nameof(Tenants));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TenantsToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _tenantService.ToggleStatusAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to update tenant status.";
        }
        else
        {
            TempData["SuccessMessage"] = "Tenant status updated successfully.";
        }
        return RedirectToAction(nameof(Tenants));
    }

    // ==========================================
    // USERS MANAGEMENT
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Users([FromQuery] FilterRequest filter, Guid? tenantId, CancellationToken cancellationToken)
    {
        var pagedUsers = await _userService.GetPagedAsync(filter, tenantId, cancellationToken);
        var availableTenants = await _tenantService.GetAllActiveAsync(cancellationToken);

        var viewModel = new AdminUsersViewModel
        {
            Users = pagedUsers,
            Filter = filter,
            SelectedTenantId = tenantId,
            AvailableTenants = availableTenants
        };
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> UsersCreate(CancellationToken cancellationToken)
    {
        var roles = await _userService.GetAllRolesAsync(cancellationToken);
        var tenants = await _tenantService.GetAllActiveAsync(cancellationToken);

        var model = new AdminUserCreateEditViewModel
        {
            Role = SystemRoles.TenantUser,
            AvailableRoles = roles,
            AvailableTenants = tenants
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsersCreate(AdminUserCreateEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await _userService.GetAllRolesAsync(cancellationToken);
            model.AvailableTenants = await _tenantService.GetAllActiveAsync(cancellationToken);
            return View(model);
        }

        var result = await _userService.CreateAsync(new CreateUserDto
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Username = model.Username,
            Password = model.Password ?? "",
            ConfirmPassword = model.ConfirmPassword ?? "",
            Role = model.Role,
            AssignedTenantIds = model.AssignedTenantIds,
            IsActive = model.IsActive
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create user.");
            model.AvailableRoles = await _userService.GetAllRolesAsync(cancellationToken);
            model.AvailableTenants = await _tenantService.GetAllActiveAsync(cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"User '{model.Username}' was created successfully.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> UsersEdit(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userService.GetAllRolesAsync(cancellationToken);
        var tenants = await _tenantService.GetAllActiveAsync(cancellationToken);

        var model = new AdminUserCreateEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Username = user.Username,
            Role = user.Roles.FirstOrDefault() ?? SystemRoles.TenantUser,
            AssignedTenantIds = user.AssignedTenants.Select(t => t.Id).ToList(),
            IsActive = user.IsActive,
            AvailableRoles = roles,
            AvailableTenants = tenants
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsersEdit(AdminUserCreateEditViewModel model, CancellationToken cancellationToken)
    {
        if (!model.Id.HasValue)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await _userService.GetAllRolesAsync(cancellationToken);
            model.AvailableTenants = await _tenantService.GetAllActiveAsync(cancellationToken);
            return View(model);
        }

        var result = await _userService.UpdateAsync(new UpdateUserDto
        {
            Id = model.Id.Value,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Username = model.Username,
            NewPassword = model.Password,
            Role = model.Role,
            AssignedTenantIds = model.AssignedTenantIds,
            IsActive = model.IsActive
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update user.");
            model.AvailableRoles = await _userService.GetAllRolesAsync(cancellationToken);
            model.AvailableTenants = await _tenantService.GetAllActiveAsync(cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"User '{model.Username}' was updated successfully.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsersToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.ToggleStatusAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to update user status.";
        }
        else
        {
            TempData["SuccessMessage"] = "User status updated successfully.";
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsersToggleLock(Guid id, [FromForm] string? reason, CancellationToken cancellationToken)
    {
        var result = await _userService.ToggleLockAsync(id, reason, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to update user lock state.";
        }
        else
        {
            TempData["SuccessMessage"] = "User account lock state updated successfully.";
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsersForcePasswordReset(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.ForcePasswordResetAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to set password reset requirement.";
        }
        else
        {
            TempData["SuccessMessage"] = "User will be required to change password on their next login.";
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsersRevokeSessions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.RevokeSessionsAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to revoke user sessions.";
        }
        else
        {
            TempData["SuccessMessage"] = "All user sessions have been revoked and the user was forced to logout.";
        }
        return RedirectToAction(nameof(Users));
    }

    // ==========================================
    // STATUSES MANAGEMENT
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Statuses(CancellationToken cancellationToken)
    {
        var statuses = await _statusService.GetAllAsync(cancellationToken);
        return View(statuses);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StatusesCreate(CreateTaskStatusDto dto, CancellationToken cancellationToken)
    {
        var result = await _statusService.CreateAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to create status.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Status '{dto.Name}' created successfully.";
        }
        return RedirectToAction(nameof(Statuses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StatusesEdit(UpdateTaskStatusDto dto, CancellationToken cancellationToken)
    {
        var result = await _statusService.UpdateAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to update status.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Status '{dto.Name}' updated successfully.";
        }
        return RedirectToAction(nameof(Statuses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StatusesToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        await _statusService.ToggleStatusAsync(id, cancellationToken);
        TempData["SuccessMessage"] = "Status updated successfully.";
        return RedirectToAction(nameof(Statuses));
    }

    // ==========================================
    // PRIORITIES MANAGEMENT
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Priorities(CancellationToken cancellationToken)
    {
        var priorities = await _priorityService.GetAllAsync(cancellationToken);
        return View(priorities);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrioritiesCreate(CreateTaskPriorityDto dto, CancellationToken cancellationToken)
    {
        var result = await _priorityService.CreateAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to create priority.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Priority '{dto.Name}' created successfully.";
        }
        return RedirectToAction(nameof(Priorities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrioritiesEdit(UpdateTaskPriorityDto dto, CancellationToken cancellationToken)
    {
        var result = await _priorityService.UpdateAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to update priority.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Priority '{dto.Name}' updated successfully.";
        }
        return RedirectToAction(nameof(Priorities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrioritiesToggleStatus(Guid id, CancellationToken cancellationToken)
    {
        await _priorityService.ToggleStatusAsync(id, cancellationToken);
        TempData["SuccessMessage"] = "Priority updated successfully.";
        return RedirectToAction(nameof(Priorities));
    }

    // ==========================================
    // SCRUM BOARDS MANAGEMENT
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ScrumBoards(CancellationToken cancellationToken)
    {
        var boards = await _scrumBoardService.GetAllBoardsAsync(null, cancellationToken);
        var statuses = await _statusService.GetAllActiveAsync(cancellationToken);
        ViewBag.Statuses = statuses;
        return View(boards);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddScrumColumn(CreateScrumColumnDto dto, Guid boardId, CancellationToken cancellationToken)
    {
        var result = await _scrumBoardService.AddColumnAsync(dto, boardId, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to add column.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Column '{dto.Name}' added successfully.";
        }
        return RedirectToAction(nameof(ScrumBoards));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateScrumColumn(UpdateScrumColumnDto dto, CancellationToken cancellationToken)
    {
        var result = await _scrumBoardService.UpdateColumnAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to update column.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Column '{dto.Name}' updated successfully.";
        }
        return RedirectToAction(nameof(ScrumBoards));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteScrumColumn(Guid id, CancellationToken cancellationToken)
    {
        var result = await _scrumBoardService.DeleteColumnAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to delete column.";
        }
        else
        {
            TempData["SuccessMessage"] = "Column removed successfully.";
        }
        return RedirectToAction(nameof(ScrumBoards));
    }

    // ==========================================
    // AUDIT LOGS VIEWER
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Audit([FromQuery] FilterRequest filter, string tab = "system", CancellationToken cancellationToken = default)
    {
        var adminAudits = await _auditService.GetAdminAuditsPagedAsync(filter, null, cancellationToken);
        var taskAudits = await _auditService.GetTaskAuditsPagedAsync(null, filter, cancellationToken);

        var viewModel = new AdminAuditViewModel
        {
            AdminAudits = adminAudits,
            TaskAudits = taskAudits,
            Filter = filter,
            ActiveTab = tab.ToLowerInvariant()
        };
        return View(viewModel);
    }
}
