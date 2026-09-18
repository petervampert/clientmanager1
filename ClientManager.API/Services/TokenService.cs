using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClientManager.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace ClientManager.API.Services;

/// <summary>
/// Generates signed JWT Bearer tokens for authenticated users.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed JWT token for the given user.
    /// </summary>
    /// <param name="user">The authenticated user. Claims are built from <see cref="User.Id"/> and <see cref="User.Username"/>.</param>
    /// <returns>
    /// A tuple containing the serialized token string and the UTC expiry <see cref="DateTime"/>.
    /// </returns>
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}

/// <inheritdoc />
public class TokenService(IConfiguration configuration) : ITokenService
{
    /// <inheritdoc />
    /// <remarks>
    /// Token claims: <c>sub</c> = user ID, <c>unique_name</c> = username, <c>jti</c> = new GUID.<br/>
    /// Signing algorithm: HMAC-SHA256.<br/>
    /// Expiry is controlled by <c>Jwt:ExpiresInHours</c> in configuration (default 8 hours).
    /// </remarks>
    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key missing.");
        var expiresInHours = int.Parse(jwtSection["ExpiresInHours"] ?? "8");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddHours(expiresInHours);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
