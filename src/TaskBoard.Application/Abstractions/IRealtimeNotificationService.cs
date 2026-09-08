using TaskBoard.Application.DTOs.Notifications;

namespace TaskBoard.Application.Abstractions;

public interface IRealtimeNotificationService
{
    Task SendTaskMovedAsync(Guid? tenantId, Guid taskId, string taskNumber, Guid targetStatusId, string oldStatus, string newStatus, string updatedBy, CancellationToken cancellationToken = default);
    Task SendTaskUpdatedAsync(Guid? tenantId, string taskNumber, string action, string updatedBy, CancellationToken cancellationToken = default);
    Task SendNotificationToUserAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default);
    Task SendForceLogoutAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
}
