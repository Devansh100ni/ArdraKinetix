namespace TaskBoard.Application.DTOs.Statuses;

public class TaskStatusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string Color { get; set; } = "#64748b";
    public bool IsActive { get; set; }
    public int TaskCount { get; set; }
}

public class CreateTaskStatusDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string Color { get; set; } = "#64748b";
    public bool IsActive { get; set; } = true;
}

public class UpdateTaskStatusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string Color { get; set; } = "#64748b";
    public bool IsActive { get; set; }
}
