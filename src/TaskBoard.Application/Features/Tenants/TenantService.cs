using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions;
using TaskBoard.Application.Abstractions.Repositories;
using TaskBoard.Application.Common;
using TaskBoard.Application.DTOs.Tenants;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Features.Tenants;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        ITenantRepository tenantRepository,
        IAdminAuditRepository adminAuditRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository;
        _adminAuditRepository = adminAuditRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        return tenant == null ? null : MapToDto(tenant);
    }

    public async Task<TenantDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByCodeAsync(code.Trim().ToUpperInvariant(), cancellationToken);
        return tenant == null ? null : MapToDto(tenant);
    }

    public async Task<IReadOnlyList<TenantDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _tenantRepository.GetAllActiveAsync(cancellationToken);
        return tenants.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<TenantDto>> GetPagedAsync(FilterRequest request, CancellationToken cancellationToken = default)
    {
        var pagedTenants = await _tenantRepository.GetPagedAsync(request, cancellationToken);
        var dtos = pagedTenants.Items.Select(MapToDto).ToList();
        return new PagedResult<TenantDto>(dtos, pagedTenants.TotalCount, pagedTenants.PageNumber, pagedTenants.PageSize);
    }

    public async Task<Result<TenantDto>> CreateAsync(CreateTenantDto dto, CancellationToken cancellationToken = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var prefix = dto.Prefix.Trim().ToUpperInvariant();

        if (await _tenantRepository.ExistsCodeAsync(code, null, cancellationToken))
        {
            return Result<TenantDto>.Failure($"A tenant with code '{code}' already exists.");
        }

        var tenant = new Tenant
        {
            Name = dto.Name.Trim(),
            Code = code,
            Prefix = prefix,
            IsActive = dto.IsActive,
            CreatedOn = DateTime.UtcNow,
            TaskSequence = new TenantTaskSequence { LastNumber = 0 }
        };

        await _tenantRepository.AddAsync(tenant, cancellationToken);

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = "TenantCreated",
            EntityType = "Tenant",
            EntityId = tenant.Id.ToString(),
            Details = $"Created tenant '{tenant.Name}' ({tenant.Code}) with prefix '{tenant.Prefix}'",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tenant {TenantId} ({TenantCode}) created successfully.", tenant.Id, tenant.Code);

        return Result<TenantDto>.Success(MapToDto(tenant));
    }

    public async Task<Result<TenantDto>> UpdateAsync(UpdateTenantDto dto, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(dto.Id, cancellationToken);
        if (tenant == null)
        {
            return Result<TenantDto>.Failure("Tenant not found.");
        }

        var code = dto.Code.Trim().ToUpperInvariant();
        var prefix = dto.Prefix.Trim().ToUpperInvariant();

        if (await _tenantRepository.ExistsCodeAsync(code, dto.Id, cancellationToken))
        {
            return Result<TenantDto>.Failure($"A tenant with code '{code}' already exists.");
        }

        var oldDetails = $"{tenant.Name} ({tenant.Code}, Prefix: {tenant.Prefix}, Active: {tenant.IsActive})";

        tenant.Name = dto.Name.Trim();
        tenant.Code = code;
        tenant.Prefix = prefix;
        tenant.IsActive = dto.IsActive;
        tenant.UpdatedOn = DateTime.UtcNow;

        _tenantRepository.Update(tenant);

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = "TenantUpdated",
            EntityType = "Tenant",
            EntityId = tenant.Id.ToString(),
            Details = $"Updated tenant from '{oldDetails}' to '{tenant.Name}' ({tenant.Code}, Prefix: {tenant.Prefix}, Active: {tenant.IsActive})",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tenant {TenantId} updated successfully.", tenant.Id);

        return Result<TenantDto>.Success(MapToDto(tenant));
    }

    public async Task<Result> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken);
        if (tenant == null)
        {
            return Result.Failure("Tenant not found.");
        }

        tenant.IsActive = !tenant.IsActive;
        tenant.UpdatedOn = DateTime.UtcNow;
        _tenantRepository.Update(tenant);

        await _adminAuditRepository.AddAsync(new AdminAudit
        {
            UserId = _currentUserService.UserId,
            Action = tenant.IsActive ? "TenantActivated" : "TenantDeactivated",
            EntityType = "Tenant",
            EntityId = tenant.Id.ToString(),
            Details = $"Tenant '{tenant.Name}' status set to Active={tenant.IsActive}",
            IpAddress = _currentUserService.IpAddress
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static TenantDto MapToDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            Prefix = tenant.Prefix,
            IsActive = tenant.IsActive,
            CreatedOn = tenant.CreatedOn,
            UpdatedOn = tenant.UpdatedOn,
            ActiveUserCount = tenant.UserTenants?.Count(ut => ut.User != null && ut.User.IsActive && !ut.User.IsDeleted) ?? 0,
            TotalTaskCount = tenant.Tasks?.Count(t => !t.IsDeleted) ?? 0,
            LastSequenceNumber = tenant.TaskSequence?.LastNumber ?? 0
        };
    }
}
