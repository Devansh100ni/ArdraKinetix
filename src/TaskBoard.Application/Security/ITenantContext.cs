namespace TaskBoard.Application.Security;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? TenantCode { get; }
    string? TenantPrefix { get; }
    bool IsGlobalAdmin { get; }
    IReadOnlyList<Guid> AllowedTenantIds { get; }
    bool HasAccessToTenant(Guid tenantId);
    void SetActiveTenant(Guid tenantId, string? code = null, string? prefix = null);
}
