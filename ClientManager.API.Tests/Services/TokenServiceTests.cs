using System.IdentityModel.Tokens.Jwt;
using ClientManager.API.Models;
using ClientManager.API.Services;
using ClientManager.API.Tests.Helpers;
using FluentAssertions;

namespace ClientManager.API.Tests.Services;

public class TokenServiceTests
{
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        var config = ConfigurationFactory.CreateJwtConfig();
        _sut = new TokenService(config);
    }

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        var user = new User { Id = 1, Username = "testuser" };

        var (token, _) = _sut.GenerateToken(user);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateToken_ExpiresInFuture()
    {
        var user = new User { Id = 1, Username = "testuser" };

        var (_, expiresAt) = _sut.GenerateToken(user);

        expiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_ExpiresApproximatelyInConfiguredHours()
    {
        var user = new User { Id = 1, Username = "testuser" };

        var (_, expiresAt) = _sut.GenerateToken(user);

        expiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(8), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        var user = new User { Id = 42, Username = "johndoe" };

        var (token, _) = _sut.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Subject.Should().Be("42");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "johndoe");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectIssuerAndAudience()
    {
        var user = new User { Id = 1, Username = "testuser" };

        var (token, _) = _sut.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateToken_TwoCallsProduceDifferentJtiClaims()
    {
        var user = new User { Id = 1, Username = "testuser" };
        var handler = new JwtSecurityTokenHandler();

        var (token1, _) = _sut.GenerateToken(user);
        var (token2, _) = _sut.GenerateToken(user);

        var jti1 = handler.ReadJwtToken(token1).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = handler.ReadJwtToken(token2).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
    }

    [Fact]
    public void GenerateToken_ThrowsWhenJwtKeyMissing()
    {
        var config = ConfigurationFactory.CreateJwtConfig(key: "");
        // Override Key to null/empty via a config with no key entry
        var emptyConfig = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var service = new TokenService(emptyConfig);
        var user = new User { Id = 1, Username = "testuser" };

        var act = () => service.GenerateToken(user);

        act.Should().Throw<InvalidOperationException>().WithMessage("*JWT Key missing*");
    }
}
