using Glyloop.API.Services;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NUnit.Framework;
using System.Security.Claims;

namespace Glyloop.API.Tests;

/// <summary>
/// Unit tests for CurrentUserService covering user identity extraction from HTTP context.
/// Tests critical security logic for accessing authenticated user information.
/// </summary>
[TestFixture]
[Category("Unit")]
public class CurrentUserServiceTests
{
    private IHttpContextAccessor _httpContextAccessor;
    private CurrentUserService _sut;

    [SetUp]
    public void SetUp()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _sut = new CurrentUserService(_httpContextAccessor);
    }

    [Test]
    public void UserId_ShouldReturnGuid_WhenValidUserIdClaimExists()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
            new Claim(ClaimTypes.Email, "user@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act
        var result = _sut.UserId;

        // Assert
        Assert.That(result, Is.EqualTo(expectedUserId));
    }

    [Test]
    public void UserId_ShouldThrowUnauthorizedAccessException_WhenHttpContextIsNull()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => { var _ = _sut.UserId; });
    }

    [Test]
    public void UserId_ShouldThrowUnauthorizedAccessException_WhenUserIsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext { User = null };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => { var _ = _sut.UserId; });
    }

    [Test]
    public void UserId_ShouldThrowUnauthorizedAccessException_WhenNameIdentifierClaimIsMissing()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.Name, "John Doe")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        _httpContextAccessor.HttpContext.Returns(httpContext);

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(() => { var _ = _sut.UserId; });
    }
}