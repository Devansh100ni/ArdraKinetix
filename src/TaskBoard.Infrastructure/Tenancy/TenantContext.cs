using TaskBoard.Application.Security;

namespace TaskBoard.Infrastructure.Tenancy;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public string? TenantCode { get; private set; }
    public string? TenantPrefix { get; private set; }
    public bool IsGlobalAdmin { get; private set; }
    public IReadOnlyList<Guid> AllowedTenantIds { get; private set; } = [];

    public void Initialize(Guid? tenantId, string? tenantCode, string? tenantPrefix, bool isGlobalAdmin, IReadOnlyList<Guid>? allowedTenantIds)
    {
        TenantId = tenantId;
        TenantCode = tenantCode;
        TenantPrefix = tenantPrefix;
        IsGlobalAdmin = isGlobalAdmin;
        AllowedTenantIds = allowedTenantIds ?? [];
    }

    public bool HasAccessToTenant(Guid tenantId)
    {
        if (IsGlobalAdmin)
        {
            return true;
        }

        if (TenantId.HasValue && TenantId.Value == tenantId)
        {
            return true;
        }

        return AllowedTenantIds.Contains(tenantId);
    }

    public void SetActiveTenant(Guid tenantId, string? code = null, string? prefix = null)
    {
        if (IsGlobalAdmin || AllowedTenantIds.Contains(tenantId))
        {
            TenantId = tenantId;
            if (!string.IsNullOrWhiteSpace(code)) TenantCode = code;
            if (!string.IsNullOrWhiteSpace(prefix)) TenantPrefix = prefix;
        }
    }
}
