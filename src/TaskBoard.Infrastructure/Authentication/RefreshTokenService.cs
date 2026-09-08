using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Authentication;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly TaskBoardDbContext _dbContext;
    private readonly int _refreshTokenExpirationDays;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        TaskBoardDbContext dbContext,
        IConfiguration configuration,
        ILogger<RefreshTokenService> logger)
    {
        _dbContext = dbContext;
        _refreshTokenExpirationDays = int.TryParse(configuration["Jwt:RefreshTokenExpirationDays"], out var days) ? days : 5;
        _logger = logger;
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToHexString(randomBytes).ToLowerInvariant(); // Clean URL-safe hex string

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresUtc = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedOn = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Issued 5-day refresh token for user {UserId}, expires {ExpiresUtc}", userId, refreshToken.ExpiresUtc);
        return refreshToken;
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var refreshToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return null;
        }

        return refreshToken;
    }

    public async Task RevokeRefreshTokenAsync(string token, string? ipAddress, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (refreshToken != null && !refreshToken.IsRevoked)
        {
            refreshToken.RevokedOn = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReasonRevoked = reason ?? "Revoked by user or system";
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Revoked refresh token for user {UserId}", refreshToken.UserId);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string? ipAddress, string? reason = null, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedOn == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var rt in activeTokens)
        {
            rt.RevokedOn = now;
            rt.RevokedByIp = ipAddress;
            rt.ReasonRevoked = reason ?? "Revoke all sessions";
        }

        if (activeTokens.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Revoked {Count} active refresh tokens for user {UserId}", activeTokens.Count, userId);
        }
    }
}
