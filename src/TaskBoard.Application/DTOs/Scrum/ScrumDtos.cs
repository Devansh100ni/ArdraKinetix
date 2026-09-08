using TaskBoard.Application.DTOs.Tasks;

namespace TaskBoard.Application.DTOs.Scrum;

public class ScrumBoardDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<ScrumBoardColumnDto> Columns { get; set; } = [];
}

public class ScrumBoardColumnDto
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public Guid StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public int? WipLimit { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<TaskDto> Tasks { get; set; } = [];
    public bool IsWipLimitExceeded => WipLimit.HasValue && Tasks.Count > WipLimit.Value;
}

public class CreateScrumBoardDto
{
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreateScrumColumnDto> Columns { get; set; } = [];
}

public class CreateScrumColumnDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public Guid StatusId { get; set; }
    public int? WipLimit { get; set; }
}

public class UpdateScrumColumnDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public Guid StatusId { get; set; }
    public int? WipLimit { get; set; }
    public bool IsActive { get; set; }
}
