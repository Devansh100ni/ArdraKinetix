namespace TaskBoard.Application.DTOs.Notifications;

public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "General"; // 'TaskAssigned', 'TaskStatusChanged', 'CommentAdded', 'SecurityAlert'
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadOn { get; set; }
    public DateTime CreatedOn { get; set; }
    public string CreatedTimeAgo => GetTimeAgo(CreatedOn);

    private static string GetTimeAgo(DateTime dt)
    {
        var span = DateTime.UtcNow - dt;
        if (span.TotalSeconds < 60) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return dt.ToString("MMM dd");
    }
}
