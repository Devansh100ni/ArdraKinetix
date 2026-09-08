using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Application.Security;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Authentication;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _secretKey;
    private readonly int _expirationMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _issuer = _configuration["Jwt:Issuer"] ?? "TaskBoardApp";
        _audience = _configuration["Jwt:Audience"] ?? "TaskBoardClients";
        _secretKey = _configuration["Jwt:SecretKey"] ?? "TaskBoard_Super_Secret_Production_Key_2026_MinLength32Chars!";
        _expirationMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var exp) ? exp : 60;
    }

    public string GenerateToken(User user, string role, Guid? primaryTenantId, IReadOnlyList<Guid> allowedTenantIds)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, role),
            new("FullName", user.FullName)
        };

        if (primaryTenantId.HasValue)
        {
            claims.Add(new Claim("TenantId", primaryTenantId.Value.ToString()));
        }

        if (allowedTenantIds != null && allowedTenantIds.Count > 0)
        {
            claims.Add(new Claim("AllowedTenants", string.Join(",", allowedTenantIds)));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateToken(string token, out Guid userId, out string role, out Guid? tenantId, out IReadOnlyList<Guid> allowedTenantIds)
    {
        userId = Guid.Empty;
        role = string.Empty;
        tenantId = null;
        allowedTenantIds = [];

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out userId))
            {
                return false;
            }

            role = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var tenantIdClaim = principal.FindFirst("TenantId")?.Value;
            if (Guid.TryParse(tenantIdClaim, out var tId))
            {
                tenantId = tId;
            }

            var allowedTenantsClaim = principal.FindFirst("AllowedTenants")?.Value;
            if (!string.IsNullOrWhiteSpace(allowedTenantsClaim))
            {
                allowedTenantIds = allowedTenantsClaim
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
