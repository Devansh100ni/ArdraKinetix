using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Application.Security;

namespace TaskBoard.Web.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
[IgnoreAntiforgeryToken]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(
        INotificationService notificationService,
        ICurrentUserService currentUserService)
    {
        _notificationService = notificationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var notifications = await _notificationService.GetUserNotificationsAsync(_currentUserService.UserId.Value, 15, cancellationToken);
        var unreadCount = await _notificationService.GetUnreadCountAsync(_currentUserService.UserId.Value, cancellationToken);

        return Ok(new
        {
            unreadCount,
            notifications
        });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _notificationService.MarkAsReadAsync(id, _currentUserService.UserId.Value, cancellationToken);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { error = result.Error });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _notificationService.MarkAllAsReadAsync(_currentUserService.UserId.Value, cancellationToken);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { error = result.Error });
    }
}
