using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Auth;
using TaskBoard.Application.DTOs.Tenants;

namespace TaskBoard.Application.Features.Tenants;

public interface ITenantService
{
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<TenantDto>> GetPagedAsync(FilterRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> CreateAsync(CreateTenantDto dto, CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> UpdateAsync(UpdateTenantDto dto, CancellationToken cancellationToken = default);
    Task<Result> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default);
}
