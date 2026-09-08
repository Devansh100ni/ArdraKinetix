using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Notifications;

namespace TaskBoard.Application.Features.Notifications;

public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, int take = 10, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateNotificationAsync(Guid userId, Guid? tenantId, string title, string message, string type, string? targetUrl = null, CancellationToken cancellationToken = default);
}
