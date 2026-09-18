using Microsoft.Extensions.Configuration;

namespace ClientManager.API.Tests.Helpers;

/// <summary>
/// Builds an <see cref="IConfiguration"/> with JWT settings suitable for unit tests.
/// </summary>
public static class ConfigurationFactory
{
    public static IConfiguration CreateJwtConfig(
        string key = "test-secret-key-for-unit-tests-32chars!!",
        string issuer = "TestIssuer",
        string audience = "TestAudience",
        int expiresInHours = 8)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"]            = key,
            ["Jwt:Issuer"]         = issuer,
            ["Jwt:Audience"]       = audience,
            ["Jwt:ExpiresInHours"] = expiresInHours.ToString()
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
