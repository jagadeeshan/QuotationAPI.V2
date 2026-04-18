using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using QuotationAPI.V2.Models.Auth;

namespace QuotationAPI.V2.Services;

public interface ITokenService
{
    string CreateAccessToken(AppUser user, IEnumerable<string> roles);
    RefreshTokenDescriptor CreateRefreshToken(AppUser user, IEnumerable<string> roles);
    ClaimsPrincipal? ValidateRefreshToken(string refreshToken);
}

public record RefreshTokenDescriptor(string Token, string TokenId, DateTime ExpiresAt);

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateAccessToken(AppUser user, IEnumerable<string> roles)
    {
        var secret = _configuration["Jwt:Key"] ?? "DEV_ONLY_SUPER_SECRET_KEY_CHANGE_ME_123456";
        var issuer = _configuration["Jwt:Issuer"] ?? "QuotationAPI.V2";
        var audience = _configuration["Jwt:Audience"] ?? "QuotationApp";
        var expiresMinutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var m) ? m : 60;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshTokenDescriptor CreateRefreshToken(AppUser user, IEnumerable<string> roles)
    {
        var secret = _configuration["Jwt:Key"] ?? "DEV_ONLY_SUPER_SECRET_KEY_CHANGE_ME_123456";
        var issuer = _configuration["Jwt:Issuer"] ?? "QuotationAPI.V2";
        var audience = _configuration["Jwt:Audience"] ?? "QuotationApp";
        var refreshTokenMinutes = int.TryParse(_configuration["Jwt:RefreshTokenMinutes"], out var rm)
            ? rm
            : 60 * 24 * 7; // 7 days by default

        var tokenId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(refreshTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("token_type", "refresh")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new RefreshTokenDescriptor(new JwtSecurityTokenHandler().WriteToken(token), tokenId, expiresAt);
    }

    public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var secret = _configuration["Jwt:Key"] ?? "DEV_ONLY_SUPER_SECRET_KEY_CHANGE_ME_123456";
        var issuer = _configuration["Jwt:Issuer"] ?? "QuotationAPI.V2";
        var audience = _configuration["Jwt:Audience"] ?? "QuotationApp";

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(refreshToken, tokenValidationParameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return null;
            }

            var tokenType = principal.FindFirst("token_type")?.Value;
            return string.Equals(tokenType, "refresh", StringComparison.OrdinalIgnoreCase) ? principal : null;
        }
        catch
        {
            return null;
        }
    }
}
