using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Notifications;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly TaskBoardDbContext _dbContext;
    private readonly IRealtimeNotificationService _realtimeService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        TaskBoardDbContext dbContext,
        IRealtimeNotificationService realtimeService,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, int take = 10, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedOn)
            .Take(take)
            .ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<Result> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (item == null)
        {
            return Result.Failure("Notification not found.");
        }

        if (!item.IsRead)
        {
            item.IsRead = true;
            item.ReadOn = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var item in unread)
        {
            item.IsRead = true;
            item.ReadOn = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<NotificationDto> CreateNotificationAsync(Guid userId, Guid? tenantId, string title, string message, string type, string? targetUrl = null, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Title = title,
            Message = message,
            Type = type,
            TargetUrl = targetUrl,
            IsRead = false,
            CreatedOn = DateTime.UtcNow
        };

        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(notification);

        // Send real-time SignalR notification
        await _realtimeService.SendNotificationToUserAsync(userId, dto, cancellationToken);

        return dto;
    }

    private static NotificationDto MapToDto(Notification n)
    {
        return new NotificationDto
        {
            Id = n.Id,
            UserId = n.UserId,
            TenantId = n.TenantId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            TargetUrl = n.TargetUrl,
            IsRead = n.IsRead,
            ReadOn = n.ReadOn,
            CreatedOn = n.CreatedOn
        };
    }
}
