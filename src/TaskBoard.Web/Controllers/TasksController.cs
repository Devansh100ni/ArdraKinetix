using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.DTOs.Attachments;
using TaskBoard.Application.DTOs.Comments;
using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.Features.Priorities;
using TaskBoard.Application.Features.Statuses;
using TaskBoard.Application.Features.Tasks;
using TaskBoard.Application.Features.Tenants;
using TaskBoard.Application.Features.Users;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Enums;
using TaskBoard.Domain.Exceptions;
using TaskBoard.Web.ViewModels;

namespace TaskBoard.Web.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly ITaskService _taskService;
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;
    private readonly ITaskStatusService _statusService;
    private readonly ITaskPriorityService _priorityService;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        ITaskService taskService,
        ITenantService tenantService,
        IUserService userService,
        ITaskStatusService statusService,
        ITaskPriorityService priorityService,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _tenantService = tenantService;
        _userService = userService;
        _statusService = statusService;
        _priorityService = priorityService;
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] TaskFilterCriteria criteria, CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GetPagedAsync(criteria, cancellationToken);
        var statuses = await _statusService.GetAllActiveAsync(cancellationToken);
        var priorities = await _priorityService.GetAllActiveAsync(cancellationToken);

        var availableTenants = _tenantContext.IsGlobalAdmin
            ? await _tenantService.GetAllActiveAsync(cancellationToken)
            : _currentUserService.IsInRole(SystemRoles.Developer)
                ? (await _tenantService.GetAllActiveAsync(cancellationToken)).Where(t => _tenantContext.AllowedTenantIds.Contains(t.Id)).ToList()
                : [];

        var effectiveTenantId = criteria.TenantId ?? _tenantContext.TenantId;
        var availableUsers = effectiveTenantId.HasValue
            ? await _userService.GetUsersByTenantAsync(effectiveTenantId.Value, cancellationToken)
            : [];

        var viewModel = new TaskListViewModel
        {
            Tasks = tasks,
            Criteria = criteria,
            AvailableTenants = availableTenants,
            AvailableStatuses = statuses,
            AvailablePriorities = priorities,
            AvailableUsers = availableUsers,
            CanCreateTask = true
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, string tab = "overview", CancellationToken cancellationToken = default)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);
        if (task == null)
        {
            return NotFound();
        }

        var isGlobalAdmin = _tenantContext.IsGlobalAdmin;
        var isDev = _currentUserService.IsInRole(SystemRoles.Developer);
        var isTenantUser = _currentUserService.IsInRole(SystemRoles.TenantUser);

        var viewModel = new TaskDetailsViewModel
        {
            Task = task,
            ActiveTab = tab.ToLowerInvariant(),
            CanEdit = isGlobalAdmin || isDev || isTenantUser,
            CanDelete = isGlobalAdmin || isDev,
            CanComment = true,
            CanUpload = true
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? parentTaskId = null, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var targetTenantId = tenantId ?? _tenantContext.TenantId;

        // For tenant user, default to their tenant
        if (!_tenantContext.IsGlobalAdmin && !_currentUserService.IsInRole(SystemRoles.Developer))
        {
            targetTenantId = _tenantContext.TenantId;
        }

        var model = new TaskCreateEditViewModel
        {
            TenantId = targetTenantId ?? Guid.Empty,
            ParentTaskId = parentTaskId
        };

        await PopulateDropdownsAsync(model, targetTenantId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskCreateEditViewModel model, CancellationToken cancellationToken)
    {
        // Enforce tenant ID security: non-admins cannot supply unauthorized tenant ID
        if (!_tenantContext.IsGlobalAdmin)
        {
            if (_currentUserService.IsInRole(SystemRoles.TenantUser))
            {
                model.TenantId = _tenantContext.TenantId ?? Guid.Empty;
            }
            else if (_currentUserService.IsInRole(SystemRoles.Developer))
            {
                if (!_tenantContext.HasAccessToTenant(model.TenantId))
                {
                    model.TenantId = _tenantContext.TenantId ?? Guid.Empty;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model, model.TenantId, cancellationToken);
            return View(model);
        }

        var result = await _taskService.CreateAsync(new CreateTaskDto
        {
            TenantId = model.TenantId,
            Title = model.Title,
            Description = model.Description,
            AssignedUserId = model.AssignedUserId,
            ParentTaskId = model.ParentTaskId,
            EstimatedHours = model.EstimatedHours,
            StartedOn = model.StartedOn,
            EndOn = model.EndOn,
            StatusId = model.StatusId,
            PriorityId = model.PriorityId
        }, cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create task.");
            await PopulateDropdownsAsync(model, model.TenantId, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Task '{result.Value.TaskNumber}' was created successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Value.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);
        if (task == null)
        {
            return NotFound();
        }

        var model = new TaskCreateEditViewModel
        {
            Id = task.Id,
            TaskNumber = task.TaskNumber,
            TenantId = task.TenantId,
            Title = task.Title,
            Description = task.Description,
            AssignedUserId = task.AssignedUserId,
            ParentTaskId = task.ParentTaskId,
            EstimatedHours = task.EstimatedHours,
            StartedOn = task.StartedOn,
            EndOn = task.EndOn,
            StatusId = task.StatusId,
            PriorityId = task.PriorityId,
            RowVersion = task.RowVersion
        };

        await PopulateDropdownsAsync(model, task.TenantId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TaskCreateEditViewModel model, CancellationToken cancellationToken)
    {
        if (!model.Id.HasValue)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model, model.TenantId, cancellationToken);
            return View(model);
        }

        try
        {
            var result = await _taskService.UpdateAsync(new UpdateTaskDto
            {
                Id = model.Id.Value,
                TenantId = model.TenantId,
                Title = model.Title,
                Description = model.Description,
                AssignedUserId = model.AssignedUserId,
                ParentTaskId = model.ParentTaskId,
                EstimatedHours = model.EstimatedHours,
                StartedOn = model.StartedOn,
                EndOn = model.EndOn,
                StatusId = model.StatusId,
                PriorityId = model.PriorityId,
                RowVersion = model.RowVersion ?? []
            }, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update task.");
                await PopulateDropdownsAsync(model, model.TenantId, cancellationToken);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Task '{result.Value.TaskNumber}' was updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.Id.Value });
        }
        catch (ConcurrencyConflictException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateDropdownsAsync(model, model.TenantId, cancellationToken);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to delete task.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = "Task was deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    // AJAX Drag & Drop Movement
    [HttpPost]
    public async Task<IActionResult> Move([FromBody] MoveTaskDto dto, CancellationToken cancellationToken)
    {
        var result = await _taskService.MoveTaskAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error });
        }

        return Json(new { success = true, task = result.Value });
    }

    // Comments AJAX
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment([FromForm] CreateTaskCommentDto dto, CancellationToken cancellationToken)
    {
        var result = await _taskService.AddCommentAsync(dto, cancellationToken);
        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error });
        }

        return Json(new { success = true, comment = result.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken cancellationToken)
    {
        var result = await _taskService.DeleteCommentAsync(commentId, cancellationToken);
        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error });
        }

        return Json(new { success = true });
    }

    // Attachments
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(Guid taskId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, error = "Please select a file to upload." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _taskService.UploadAttachmentAsync(new UploadAttachmentDto
        {
            TaskId = taskId,
            FileStream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType
        }, cancellationToken);

        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error });
        }

        return Json(new { success = true, attachment = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.GetAttachmentFileAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value.stream == null)
        {
            return NotFound();
        }

        return File(result.Value.stream, result.Value.contentType, result.Value.fileName);
    }

    [HttpGet]
    public async Task<IActionResult> PreviewAttachment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.GetAttachmentFileAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Value.stream == null)
        {
            return NotFound();
        }

        return File(result.Value.stream, result.Value.contentType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(Guid id, CancellationToken cancellationToken)
    {
        var result = await _taskService.DeleteAttachmentAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return Json(new { success = false, error = result.Error });
        }

        return Json(new { success = true });
    }

    private async Task PopulateDropdownsAsync(TaskCreateEditViewModel model, Guid? tenantId, CancellationToken cancellationToken)
    {
        model.AvailableStatuses = await _statusService.GetAllActiveAsync(cancellationToken);
        model.AvailablePriorities = await _priorityService.GetAllActiveAsync(cancellationToken);

        if (_tenantContext.IsGlobalAdmin)
        {
            model.AvailableTenants = await _tenantService.GetAllActiveAsync(cancellationToken);
        }
        else if (_currentUserService.IsInRole(SystemRoles.Developer))
        {
            var all = await _tenantService.GetAllActiveAsync(cancellationToken);
            model.AvailableTenants = all.Where(t => _tenantContext.AllowedTenantIds.Contains(t.Id)).ToList();
        }

        var effectiveTenantId = tenantId ?? _tenantContext.TenantId;
        if (effectiveTenantId.HasValue)
        {
            model.AvailableUsers = await _userService.GetUsersByTenantAsync(effectiveTenantId.Value, cancellationToken);
            var tasksPaged = await _taskService.GetPagedAsync(new TaskFilterCriteria
            {
                TenantId = effectiveTenantId.Value,
                PageSize = 100,
                OnlyParentTasks = true
            }, cancellationToken);
            model.AvailableParentTasks = tasksPaged.Items.Where(t => !model.Id.HasValue || t.Id != model.Id.Value).ToList();
        }
    }
}
