namespace TaskBoard.Application.DTOs.Priorities;

public class TaskPriorityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string Color { get; set; } = "#64748b";
    public bool IsActive { get; set; }
    public int TaskCount { get; set; }
}

public class CreateTaskPriorityDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string Color { get; set; } = "#64748b";
    public bool IsActive { get; set; } = true;
}

public class UpdateTaskPriorityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string Color { get; set; } = "#64748b";
    public bool IsActive { get; set; }
}
