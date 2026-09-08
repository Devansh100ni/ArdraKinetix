using TaskBoard.Application.DTOs.Dashboard;
using TaskBoard.Application.DTOs.Scrum;
using TaskBoard.Application.DTOs.Tenants;

namespace TaskBoard.Web.ViewModels;

public class DashboardViewModel
{
    public string Role { get; set; } = string.Empty;
    public AdminDashboardDto? AdminDashboard { get; set; }
    public DeveloperDashboardDto? DeveloperDashboard { get; set; }
    public TenantDashboardDto? TenantDashboard { get; set; }
    public IReadOnlyList<TenantDto> AvailableTenants { get; set; } = [];
    public Guid? SelectedTenantId { get; set; }
}

public class ScrumBoardViewModel
{
    public ScrumBoardDto? Board { get; set; }
    public IReadOnlyList<TenantDto> AvailableTenants { get; set; } = [];
    public Guid? SelectedTenantId { get; set; }
    public string? SelectedTenantName { get; set; }
    public bool CanCreateTask { get; set; }
}
