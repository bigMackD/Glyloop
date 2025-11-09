using AutoFixture;
using Glyloop.API.Controllers;
using Glyloop.Application.Common.Interfaces;
using Glyloop.Domain.Common;
using Glyloop.Infrastructure.Services.Identity;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.Security.Claims;

namespace Glyloop.API.Tests;

/// <summary>
/// Common test utilities and builders for API layer unit tests.
/// Provides reusable setup patterns and test data generation.
/// </summary>
public static class TestUtilities
{
    /// <summary>
    /// Creates a configured IFixture instance for test data generation.
    /// </summary>
    public static IFixture CreateFixture()
    {
        return new Fixture();
    }

    /// <summary>
    /// Creates a mock JWT token service with default behavior.
    /// </summary>
    public static IJwtTokenService CreateJwtTokenService()
    {
        var service = Substitute.For<IJwtTokenService>();
        service.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns("mock-access-token");
        service.GenerateRefreshToken()
            .Returns("mock-refresh-token");
        service.ValidateToken(Arg.Any<string>())
            .Returns(new ClaimsPrincipal());
        return service;
    }

    /// <summary>
    /// Creates a mock IMediator with default success behavior.
    /// </summary>
    public static IMediator CreateMediator()
    {
        var mediator = Substitute.For<IMediator>();
        // Configure default success responses for common commands
        return mediator;
    }

    /// <summary>
    /// Creates a mock IConfiguration with JWT settings.
    /// </summary>
    public static IConfiguration CreateConfiguration()
    {
        var configuration = Substitute.For<IConfiguration>();

        // JWT Settings
        var jwtSection = Substitute.For<IConfigurationSection>();
        jwtSection["SecretKey"].Returns("test-secret-key-12345678901234567890");
        jwtSection["Issuer"].Returns("test-issuer");
        jwtSection["Audience"].Returns("test-audience");
        jwtSection.GetValue<int>("AccessTokenExpirationMinutes", Arg.Any<int>()).Returns(15);
        jwtSection.GetValue<int>("RefreshTokenExpirationDays", Arg.Any<int>()).Returns(7);
        configuration.GetSection("JwtSettings").Returns(jwtSection);

        return configuration;
    }

    /// <summary>
    /// Creates an AuthController with mocked dependencies.
    /// </summary>
    public static AuthController CreateAuthController(
        IMediator? mediator = null,
        IJwtTokenService? jwtTokenService = null,
        IConfiguration? configuration = null)
    {
        return new AuthController(
            mediator ?? CreateMediator(),
            jwtTokenService ?? CreateJwtTokenService(),
            configuration ?? CreateConfiguration());
    }

    /// <summary>
    /// Creates an HttpContext with the specified user claims.
    /// </summary>
    public static DefaultHttpContext CreateHttpContextWithUser(Guid userId, string email = "test@example.com")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext { User = principal };
    }

    /// <summary>
    /// Creates an HttpContext with service provider containing the specified identity service.
    /// </summary>
    public static DefaultHttpContext CreateHttpContextWithIdentityService(IIdentityService identityService)
    {
        var httpContext = new DefaultHttpContext();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IIdentityService)).Returns(identityService);
        httpContext.RequestServices = serviceProvider;
        return httpContext;
    }

    /// <summary>
    /// Creates a successful Result<T> with the specified value.
    /// </summary>
    public static Result<T> CreateSuccessResult<T>(T value)
    {
        return Result.Success(value);
    }

    /// <summary>
    /// Creates a failed Result<T> with the specified error.
    /// </summary>
    public static Result<T> CreateFailureResult<T>(Error error)
    {
        return Result.Failure<T>(error);
    }

    /// <summary>
    /// Creates an Error with the specified code and message.
    /// </summary>
    public static Error CreateError(string code, string message)
    {
        return Error.Create(code, message);
    }

    /// <summary>
    /// Creates a mock IIdentityService with default successful behavior.
    /// </summary>
    public static IIdentityService CreateIdentityService()
    {
        var service = Substitute.For<IIdentityService>();
        var userId = Guid.NewGuid();
        var email = "test@example.com";

        // Default successful responses
        service.ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success((userId, email)));

        service.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(userId));

        return service;
    }

    /// <summary>
    /// Sets up ControllerContext with the specified HttpContext.
    /// </summary>
    public static TController SetupControllerContext<TController>(TController controller, HttpContext httpContext)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    /// <summary>
    /// Extension method to easily set up controller context.
    /// </summary>
    public static TController WithHttpContext<TController>(this TController controller, HttpContext httpContext)
        where TController : ControllerBase
    {
        return SetupControllerContext(controller, httpContext);
    }
}

/// <summary>
/// Common test data builders for creating valid request/response objects.
/// </summary>
public static class TestDataBuilders
{
    public static Glyloop.API.Contracts.Auth.LoginRequest CreateValidLoginRequest(
        string email = "user@example.com",
        string password = "ValidPassword123!")
    {
        return new Glyloop.API.Contracts.Auth.LoginRequest
        {
            Email = email,
            Password = password
        };
    }

    public static Glyloop.API.Contracts.Auth.RegisterRequest CreateValidRegisterRequest(
        string email = "newuser@example.com",
        string password = "StrongPassword123!")
    {
        return new Glyloop.API.Contracts.Auth.RegisterRequest
        {
            Email = email,
            Password = password
        };
    }

    public static Glyloop.API.Contracts.Events.CreateFoodEventRequest CreateValidFoodEventRequest()
    {
        return new Glyloop.API.Contracts.Events.CreateFoodEventRequest
        {
            EventTime = DateTimeOffset.UtcNow,
            CarbohydratesGrams = 45,
            MealTagId = 1,
            AbsorptionHint = "rapid",
            Note = "Breakfast carbs"
        };
    }

    public static Glyloop.API.Contracts.Events.CreateInsulinEventRequest CreateValidInsulinEventRequest()
    {
        return new Glyloop.API.Contracts.Events.CreateInsulinEventRequest
        {
            EventTime = DateTimeOffset.UtcNow,
            InsulinType = "fast",
            InsulinUnits = 8.5m,
            Preparation = "Mixed with water",
            Delivery = "Subcutaneous injection",
            Timing = "Before meal",
            Note = "Morning dose"
        };
    }

    public static Glyloop.API.Contracts.Events.CreateExerciseEventRequest CreateValidExerciseEventRequest()
    {
        return new Glyloop.API.Contracts.Events.CreateExerciseEventRequest
        {
            EventTime = DateTimeOffset.UtcNow,
            ExerciseTypeId = 1,
            DurationMinutes = 45,
            Intensity = "vigorous",
            Note = "Morning run"
        };
    }

    public static Glyloop.API.Contracts.Events.CreateNoteEventRequest CreateValidNoteEventRequest()
    {
        return new Glyloop.API.Contracts.Events.CreateNoteEventRequest
        {
            EventTime = DateTimeOffset.UtcNow,
            NoteText = "Feeling good today"
        };
    }
}
