using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Security;

public interface IRefreshTokenService
{
    Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<RefreshToken?> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string token, string? ipAddress, string? reason = null, CancellationToken cancellationToken = default);
    Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress, string? reason = null, CancellationToken cancellationToken = default);
}
