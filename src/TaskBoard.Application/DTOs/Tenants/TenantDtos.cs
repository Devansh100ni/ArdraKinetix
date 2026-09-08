namespace TaskBoard.Application.DTOs.Tenants;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public int ActiveUserCount { get; set; }
    public int TotalTaskCount { get; set; }
    public long LastSequenceNumber { get; set; }
}

public class CreateTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UpdateTenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
