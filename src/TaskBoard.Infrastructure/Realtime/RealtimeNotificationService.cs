using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.DTOs.Notifications;

namespace TaskBoard.Infrastructure.Realtime;

public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<TaskBoardHub> _hubContext;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(
        IHubContext<TaskBoardHub> hubContext,
        ILogger<RealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendTaskMovedAsync(Guid? tenantId, Guid taskId, string taskNumber, Guid targetStatusId, string oldStatus, string newStatus, string updatedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                taskId,
                taskNumber,
                targetStatusId,
                oldStatus,
                newStatus,
                updatedBy,
                tenantId,
                timestamp = DateTime.UtcNow
            };

            if (tenantId.HasValue)
            {
                var g1 = $"tenant_{tenantId.Value}";
                var g2 = $"tenant_{tenantId.Value:D}";
                var g3 = $"tenant_{tenantId.Value.ToString().ToLowerInvariant()}";
                var gAdmin = "global_admin";

                await _hubContext.Clients.Groups(new[] { g1, g2, g3, gAdmin }).SendAsync("TaskMoved", payload, cancellationToken);
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("TaskMoved", payload, cancellationToken);
            }

            _logger.LogInformation("SignalR TaskMoved broadcasted for {TaskNumber} (Id: {TaskId}) to status {TargetStatusId} ({NewStatus})", taskNumber, taskId, targetStatusId, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast TaskMoved for {TaskNumber}", taskNumber);
        }
    }

    public async Task SendTaskUpdatedAsync(Guid? tenantId, string taskNumber, string action, string updatedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                taskNumber,
                action,
                updatedBy,
                timestamp = DateTime.UtcNow
            };

            if (tenantId.HasValue)
            {
                await _hubContext.Clients.Group($"tenant_{tenantId.Value}").SendAsync("TaskUpdated", payload, cancellationToken);
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("TaskUpdated", payload, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast TaskUpdated for {TaskNumber}", taskNumber);
        }
    }

    public async Task SendNotificationToUserAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var userGroup1 = $"user_{userId}";
            var userGroup2 = $"user_{userId:D}";
            var userGroup3 = $"user_{userId.ToString().ToLowerInvariant()}";

            await _hubContext.Clients.Groups(new[] { userGroup1, userGroup2, userGroup3 })
                .SendAsync("ReceiveNotification", notification, cancellationToken);

            _logger.LogInformation("SignalR Notification sent to user {UserId}: {Title}", userId, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
        }
    }

    public async Task SendForceLogoutAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                reason,
                timestamp = DateTime.UtcNow
            };

            var userGroup1 = $"user_{userId}";
            var userGroup2 = $"user_{userId:D}";
            var userGroup3 = $"user_{userId.ToString().ToLowerInvariant()}";

            await _hubContext.Clients.Groups(new[] { userGroup1, userGroup2, userGroup3 })
                .SendAsync("ForceLogout", payload, cancellationToken);

            _logger.LogInformation("SignalR ForceLogout broadcasted to user {UserId}. Reason: {Reason}", userId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast ForceLogout to user {UserId}", userId);
        }
    }
}
