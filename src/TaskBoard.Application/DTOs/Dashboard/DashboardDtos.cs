using TaskBoard.Application.DTOs.Tasks;
using TaskBoard.Application.DTOs.Tenants;

namespace TaskBoard.Application.DTOs.Dashboard;

public class AdminDashboardDto
{
    public int TotalTenants { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalTasks { get; set; }
    public int OverdueTasksCount { get; set; }
    public Dictionary<string, int> TasksByStatus { get; set; } = new();
    public Dictionary<string, int> TasksByPriority { get; set; } = new();
    public IReadOnlyList<TenantDto> RecentTenants { get; set; } = [];
    public IReadOnlyList<TaskDto> RecentTasks { get; set; } = [];
    public IReadOnlyList<TaskDto> OverdueTasks { get; set; } = [];
}

public class DeveloperDashboardDto
{
    public int AssignedTenantsCount { get; set; }
    public int MyAssignedTasksCount { get; set; }
    public int HighPriorityTasksCount { get; set; }
    public int OverdueTasksCount { get; set; }
    public IReadOnlyList<TenantDto> AssignedTenants { get; set; } = [];
    public Dictionary<string, int> TasksByStatus { get; set; } = new();
    public IReadOnlyList<TaskDto> MyTasks { get; set; } = [];
    public IReadOnlyList<TaskDto> RecentTasks { get; set; } = [];
}

public class TenantDashboardDto
{
    public string TenantName { get; set; } = string.Empty;
    public string TenantCode { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int OpenTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasksCount { get; set; }
    public Dictionary<string, int> TasksByStatus { get; set; } = new();
    public Dictionary<string, int> TasksByPriority { get; set; } = new();
    public IReadOnlyList<TaskDto> RecentTasks { get; set; } = [];
}
