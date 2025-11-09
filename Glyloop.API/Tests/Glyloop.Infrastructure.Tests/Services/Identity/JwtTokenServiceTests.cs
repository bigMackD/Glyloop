using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Glyloop.Infrastructure.Services.Identity;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Glyloop.Infrastructure.Tests.Services.Identity;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category("Unit")]
public class JwtTokenServiceTests
{
    private static IConfiguration BuildConfig(string? secretOverride = null, int? expOverride = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = secretOverride ?? "0123456789abcdef0123456789abcdef0123456789abcdef",
            ["JwtSettings:Issuer"] = "glyloop.test",
            ["JwtSettings:Audience"] = "glyloop.clients",
            ["JwtSettings:AccessTokenExpirationMinutes"] = (expOverride ?? 5).ToString()
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }

    [Test]
    public void GenerateAndValidate_ShouldRoundtripClaims()
    {
        var config = BuildConfig();
        var svc = new JwtTokenService(config);
        var userId = Guid.NewGuid();
        var email = "user@example.com";

        var token = svc.GenerateAccessToken(userId, email);
        var principal = svc.ValidateToken(token);
        Assert.That(principal, Is.Not.Null);

        // Validate issuer/audience/expiry via decode (no validation here)
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Multiple(() =>
        {
            Assert.That(jwt.Issuer, Is.EqualTo("glyloop.test"));
            Assert.That(jwt.Audiences.Single(), Is.EqualTo("glyloop.clients"));
            Assert.That(jwt.ValidTo, Is.GreaterThan(DateTime.UtcNow.AddMinutes(3)));
            Assert.That(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value, Is.EqualTo(userId.ToString()));
            Assert.That(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value, Is.EqualTo(email));
        });
    }

    [Test]
    public void ValidateToken_ShouldReturnNull_ForInvalidToken()
    {
        var config = BuildConfig();
        var svc = new JwtTokenService(config);

        var principal = svc.ValidateToken("invalid.token");

        Assert.That(principal, Is.Null);
    }

    [Test]
    public void GenerateRefreshToken_ShouldBeBase64_AndRandom()
    {
        var config = BuildConfig();
        var svc = new JwtTokenService(config);

        var t1 = svc.GenerateRefreshToken();
        var t2 = svc.GenerateRefreshToken();

        var bytes1 = Convert.FromBase64String(t1);
        var bytes2 = Convert.FromBase64String(t2);

        Assert.Multiple(() =>
        {
            Assert.That(bytes1.Length, Is.EqualTo(32));
            Assert.That(bytes2.Length, Is.EqualTo(32));
            Assert.That(t1, Is.Not.EqualTo(t2));
        });
    }
}


