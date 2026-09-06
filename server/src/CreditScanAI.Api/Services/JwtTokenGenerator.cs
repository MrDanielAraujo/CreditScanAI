using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CreditScanAI.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CreditScanAI.Api.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}

/// <summary>
/// Issues the JWT a client uses against every other endpoint once Fase 8
/// Parte 2 turns on [Authorize] everywhere. 24h expiry per
/// 10_CASOS_DE_USO.md UC-01 - no refresh-token flow yet, matching this
/// project's precedent of building the minimal real thing first.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private const int TokenLifetimeHours = 24;

    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration) => _configuration = configuration;

    public string GenerateToken(User user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada.");

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtClaimNames.TenantId, user.TenantId.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        ];

        if (!string.IsNullOrWhiteSpace(user.Name))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.Name));
        }

        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(TokenLifetimeHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class JwtClaimNames
{
    public const string TenantId = "tenant_id";
}
