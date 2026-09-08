namespace TaskBoard.Application.DTOs.Audit;

public class TaskAuditDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string TaskNumber { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? IpAddress { get; set; }
}

public class AdminAuditDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? IpAddress { get; set; }
}
