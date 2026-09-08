using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Security;

public interface IJwtTokenService
{
    string GenerateToken(User user, string role, Guid? primaryTenantId, IReadOnlyList<Guid> allowedTenantIds);
    bool ValidateToken(string token, out Guid userId, out string role, out Guid? tenantId, out IReadOnlyList<Guid> allowedTenantIds);
}
