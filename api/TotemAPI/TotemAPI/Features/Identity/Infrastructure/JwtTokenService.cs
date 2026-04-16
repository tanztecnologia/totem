using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TotemAPI.Features.Identity.Application.Abstractions;
using TotemAPI.Features.Identity.Domain;
using TotemAPI.Infrastructure.Auth;

namespace TotemAPI.Features.Identity.Infrastructure;

public sealed class JwtTokenService : IJwtTokenService
{
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    private readonly JwtOptions _options;

    public string CreateToken(User user)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_options.Key);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString("D")),
            new("tenant_id", user.TenantId.ToString("D")),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        foreach (var perm in Permissions.ForRole(user.Role))
        {
            claims.Add(new Claim(ClaimsPrincipalExtensions.PermissionClaimType, perm));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
