using AutoFixture;
using Glyloop.API.Contracts.Auth;
using Glyloop.API.Controllers;
using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Common;
using Glyloop.Infrastructure.Services.Identity;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;
using System.Security.Claims;

namespace Glyloop.API.Tests;

/// <summary>
/// Unit tests for AuthController covering authentication and registration scenarios.
/// Tests critical business logic including login flow, error handling, and security.
/// </summary>
[TestFixture]
[Category("Unit")]
public class AuthControllerTests
{
    private IFixture _fixture;
    private IMediator _mediator;
    private IJwtTokenService _jwtTokenService;
    private IConfiguration _configuration;
    private AuthController _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
        _mediator = Substitute.For<IMediator>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "test-secret-key-12345678901234567890",
            ["JwtSettings:Issuer"] = "test-issuer",
            ["JwtSettings:Audience"] = "test-audience",
            ["JwtSettings:AccessTokenExpirationMinutes"] = "15",
            ["JwtSettings:RefreshTokenExpirationDays"] = "7"
        });
        _configuration = configurationBuilder.Build();

        _sut = new AuthController(_mediator, _jwtTokenService, _configuration);
    }

    #region Login Tests

    [Test]
    public async Task Login_ShouldReturnOkWithTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "ValidPassword123!"
        };

        var userId = Guid.NewGuid();
        var email = request.Email;
        var expectedAccessToken = "access-token-123";
        var expectedRefreshToken = "refresh-token-456";

        // Setup identity service mock
        var identityService = Substitute.For<IIdentityService>();
        identityService.ValidateCredentialsAsync(request.Email, request.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Success((userId, email)));

        // Setup JWT token service mocks
        _jwtTokenService.GenerateAccessToken(userId, email).Returns(expectedAccessToken);
        _jwtTokenService.GenerateRefreshToken().Returns(expectedRefreshToken);

        // Setup HttpContext with service provider
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = Substitute.For<IServiceProvider>();
        httpContext.RequestServices.GetService(typeof(IIdentityService)).Returns(identityService);
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _sut.Login(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result!;
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var response = (LoginResponse)okResult.Value!;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.UserId, Is.EqualTo(userId));
        Assert.That(response.Email, Is.EqualTo(email));

        // Verify tokens were generated
        _jwtTokenService.Received(1).GenerateAccessToken(userId, email);
        _jwtTokenService.Received(1).GenerateRefreshToken();
    }

    [Test]
    public async Task Login_ShouldReturnUnauthorizedWithAccountLocked_WhenAccountIsLocked()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "locked@example.com",
            Password = "AnyPassword123!"
        };

        var error = Error.Create("Auth.AccountLockedOut", "Account is temporarily locked due to multiple failed login attempts");

        // Setup identity service mock to return locked account error
        var identityService = Substitute.For<IIdentityService>();
        identityService.ValidateCredentialsAsync(request.Email, request.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<(Guid, string)>(error));

        // Setup HttpContext with service provider
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = Substitute.For<IServiceProvider>();
        httpContext.RequestServices.GetService(typeof(IIdentityService)).Returns(identityService);
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _sut.Login(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = (UnauthorizedObjectResult)result.Result!;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(401));

        var problemDetails = (ProblemDetails)unauthorizedResult.Value!;
        Assert.That(problemDetails.Title, Is.EqualTo("Account Locked"));
        Assert.That(problemDetails.Detail, Contains.Substring("Account is temporarily locked"));
        Assert.That(problemDetails.Status, Is.EqualTo(401));
    }

    [Test]
    public async Task Login_ShouldReturnUnauthorizedWithInvalidCredentials_WhenCredentialsAreInvalid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword123!"
        };

        var error = Error.Create("Auth.InvalidCredentials", "Invalid email or password");

        // Setup identity service mock to return invalid credentials error
        var identityService = Substitute.For<IIdentityService>();
        identityService.ValidateCredentialsAsync(request.Email, request.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<(Guid, string)>(error));

        // Setup HttpContext with service provider
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = Substitute.For<IServiceProvider>();
        httpContext.RequestServices.GetService(typeof(IIdentityService)).Returns(identityService);
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _sut.Login(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = (UnauthorizedObjectResult)result.Result!;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(401));

        var problemDetails = (ProblemDetails)unauthorizedResult.Value!;
        Assert.That(problemDetails.Title, Is.EqualTo("Authentication Failed"));
        Assert.That(problemDetails.Detail, Contains.Substring("Invalid email or password"));
        Assert.That(problemDetails.Status, Is.EqualTo(401));
    }

    #endregion

    #region Register Tests

    [Test]
    public async Task Register_ShouldReturnCreatedWithUserResponse_WhenRegistrationSucceeds()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "StrongPassword123!"
        };

        var userId = Guid.NewGuid();
        var userRegisteredDto = new Glyloop.Application.DTOs.Account.UserRegisteredDto(userId, request.Email, DateTimeOffset.UtcNow);

        // Setup MediatR mock to return success
        _mediator.Send(Arg.Any<Glyloop.Application.Commands.Account.Register.RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(userRegisteredDto));

        // Act
        var result = await _sut.Register(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.That(createdResult.StatusCode, Is.EqualTo(201));
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(_sut.Register)));

        var response = (RegisterResponse)createdResult.Value!;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.UserId, Is.EqualTo(userId));
        Assert.That(response.Email, Is.EqualTo(request.Email));
    }

    [Test]
    public async Task Register_ShouldReturnConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "AnyPassword123!"
        };

        var error = Error.Create("User.EmailAlreadyExists", "A user with this email address already exists");

        // Setup MediatR mock to return email exists error
        _mediator.Send(Arg.Any<Glyloop.Application.Commands.Account.Register.RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Glyloop.Application.DTOs.Account.UserRegisteredDto>(error));

        // Act
        var result = await _sut.Register(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ConflictObjectResult>());
        var conflictResult = (ConflictObjectResult)result.Result!;
        Assert.That(conflictResult.StatusCode, Is.EqualTo(409));

        var problemDetails = (ProblemDetails)conflictResult.Value!;
        Assert.That(problemDetails.Title, Is.EqualTo("Email Already Exists"));
        Assert.That(problemDetails.Detail, Contains.Substring("already exists"));
        Assert.That(problemDetails.Status, Is.EqualTo(409));
    }

    [Test]
    public async Task Register_ShouldReturnBadRequest_WhenRegistrationFailsWithGenericError()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "weak"
        };

        var error = Error.Create("Auth.WeakPassword", "Password does not meet security requirements");

        // Setup MediatR mock to return validation error
        _mediator.Send(Arg.Any<Glyloop.Application.Commands.Account.Register.RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Glyloop.Application.DTOs.Account.UserRegisteredDto>(error));

        // Act
        var result = await _sut.Register(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        var badRequestResult = (BadRequestObjectResult)result.Result!;
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));

        var problemDetails = (ProblemDetails)badRequestResult.Value!;
        Assert.That(problemDetails.Title, Is.EqualTo("Registration Failed"));
        Assert.That(problemDetails.Detail, Contains.Substring("security requirements"));
        Assert.That(problemDetails.Status, Is.EqualTo(400));
    }

    #endregion

    #region Logout Tests

    [Test]
    public void Logout_ShouldReturnOk_WhenUserIsLoggedIn()
    {
        // Arrange
        // Setup HttpContext with response (cookies will be cleared)
        var httpContext = new DefaultHttpContext();
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.Logout();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var response = okResult.Value;
        Assert.That(response, Is.Not.Null);
        // Check that the response contains the expected message
        var messageProperty = response.GetType().GetProperty("message");
        Assert.That(messageProperty, Is.Not.Null);
        var messageValue = messageProperty.GetValue(response);
        Assert.That(messageValue, Is.EqualTo("Logged out successfully"));
    }

    #endregion

    #region GetSession Tests

    [Test]
    public void GetSession_ShouldReturnOkWithUserInfo_WhenClaimsAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@example.com";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.GetSession();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var response = (SessionResponse)okResult.Value!;
        Assert.That(response.UserId, Is.EqualTo(userId));
        Assert.That(response.Email, Is.EqualTo(email));
    }

    [Test]
    public void GetSession_ShouldReturnUnauthorized_WhenUserIdClaimIsInvalid()
    {
        // Arrange
        var invalidUserId = "not-a-guid";
        var email = "user@example.com";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, invalidUserId),
            new Claim(ClaimTypes.Email, email)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.GetSession();

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(401));

        var problemDetails = (ProblemDetails)unauthorizedResult.Value!;
        Assert.That(problemDetails.Title, Is.EqualTo("Invalid Session"));
        Assert.That(problemDetails.Detail, Contains.Substring("invalid"));
        Assert.That(problemDetails.Status, Is.EqualTo(401));
    }

    [Test]
    public void GetSession_ShouldReturnUnauthorized_WhenClaimsAreMissing()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // Empty claims

        var httpContext = new DefaultHttpContext { User = principal };
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.GetSession();

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(401));

        var problemDetails = (ProblemDetails)unauthorizedResult.Value!;
        Assert.That(problemDetails.Title, Is.EqualTo("Invalid Session"));
        Assert.That(problemDetails.Detail, Contains.Substring("invalid"));
        Assert.That(problemDetails.Status, Is.EqualTo(401));
    }

    #endregion

    #region Refresh Tests

    [Test]
    public void Refresh_ShouldReturnOkWithNewTokens_WhenValidRefreshTokenProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        var refreshToken = "valid-refresh-token";
        var newAccessToken = "new-access-token";
        var newRefreshToken = "new-refresh-token";

        // Create a valid JWT token with user claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        };

        // Mock JWT service to return valid principal
        _jwtTokenService.ValidateToken(Arg.Any<string>()).Returns(new ClaimsPrincipal(new ClaimsIdentity(claims)));
        _jwtTokenService.GenerateAccessToken(userId, email).Returns(newAccessToken);
        _jwtTokenService.GenerateRefreshToken().Returns(newRefreshToken);

        // Setup HttpContext with refresh token cookie
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["access_token"] = "expired-access-token"
        });
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.Refresh();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var response = (RefreshResponse)okResult.Value!;
        Assert.That(response.UserId, Is.EqualTo(userId));
        Assert.That(response.Email, Is.EqualTo(email));

        // Verify new tokens were generated
        _jwtTokenService.Received(1).ValidateToken(Arg.Any<string>());
        _jwtTokenService.Received(1).GenerateAccessToken(userId, email);
        _jwtTokenService.Received(1).GenerateRefreshToken();
    }

    [Test]
    public void Refresh_ShouldReturnUnauthorized_WhenRefreshTokenIsMissing()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>()); // Empty cookies
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.Refresh();

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(401));

        var problemDetails = (ProblemDetails)unauthorizedResult.Value!;
        Assert.That(problemDetails.Title, Is.EqualTo("Unauthorized"));
        Assert.That(problemDetails.Detail, Contains.Substring("Refresh token not found"));
        Assert.That(problemDetails.Status, Is.EqualTo(401));
    }

    [Test]
    public void Refresh_ShouldReturnUnauthorized_WhenAccessTokenIsMissing()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken
        });
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.Refresh();

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(401));

        var problemDetails = (ProblemDetails)unauthorizedResult.Value!;
        Assert.That(problemDetails.Title, Is.EqualTo("Unauthorized"));
        Assert.That(problemDetails.Detail, Contains.Substring("Access token not found"));
        Assert.That(problemDetails.Status, Is.EqualTo(401));
    }

    [Test]
    public void Refresh_ShouldReturnUnauthorized_WhenTokenValidationFails()
    {
        // Arrange
        var refreshToken = "some-token";
        var invalidAccessToken = "invalid-access-token";

        // Mock JWT service to return null (invalid token)
        _jwtTokenService.ValidateToken(invalidAccessToken).Returns((ClaimsPrincipal?)null);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["access_token"] = invalidAccessToken
        });
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = _sut.Refresh();

        // Assert
        Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        var unauthorizedResult = (UnauthorizedObjectResult)result;
        Assert.That(unauthorizedResult.StatusCode, Is.EqualTo(401));

        var problemDetails = (ProblemDetails)unauthorizedResult.Value!;
        Assert.That(problemDetails.Title, Is.EqualTo("Unauthorized"));
        Assert.That(problemDetails.Detail, Contains.Substring("Invalid token"));
        Assert.That(problemDetails.Status, Is.EqualTo(401));
    }

    #endregion
}

// Test helper class for cookie collections
internal class TestCookieCollection : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies;

    public TestCookieCollection(Dictionary<string, string> cookies)
    {
        _cookies = cookies;
    }

    public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;

    public int Count => _cookies.Count;

    public ICollection<string> Keys => _cookies.Keys;

    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

    public bool TryGetValue(string key, out string? value) => _cookies.TryGetValue(key, out value);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
