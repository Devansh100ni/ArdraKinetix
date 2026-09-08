using TaskBoard.Application.DTOs.Dashboard;

namespace TaskBoard.Application.Features.Dashboard;

public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
    Task<DeveloperDashboardDto> GetDeveloperDashboardAsync(CancellationToken cancellationToken = default);
    Task<TenantDashboardDto> GetTenantDashboardAsync(CancellationToken cancellationToken = default);
}
