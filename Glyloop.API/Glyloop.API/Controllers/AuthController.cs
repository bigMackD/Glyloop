using Glyloop.API.Contracts.Auth;
using Glyloop.API.Mapping;
using Glyloop.Infrastructure.Services.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glyloop.API.Controllers;

/// <summary>
/// Handles user authentication operations including registration, login, logout, and token refresh.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        IMediator mediator,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">Registration details including email and password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registered user information</returns>
    /// <response code="201">User successfully registered</response>
    /// <response code="400">Invalid input or email already exists</response>
    /// <response code="409">Email already registered</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "User.EmailAlreadyExists")
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Email Already Exists",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status409Conflict
                });
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Registration Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = result.Value.ToResponse();
        return CreatedAtAction(nameof(Register), new { id = response.UserId }, response);
    }

    /// <summary>
    /// Authenticates a user and sets JWT tokens in httpOnly cookies.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information (tokens are set in cookies)</returns>
    /// <response code="200">Login successful, JWT tokens set in cookies</response>
    /// <response code="400">Invalid credentials</response>
    /// <response code="401">Authentication failed or account locked</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        // Validate credentials using Identity Service
        var identityService = HttpContext.RequestServices
            .GetRequiredService<Application.Common.Interfaces.IIdentityService>();

        var validationResult = await identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (validationResult.IsFailure)
        {
            if (validationResult.Error.Code == "Auth.AccountLockedOut")
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Account Locked",
                    Detail = validationResult.Error.Message,
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication Failed",
                Detail = validationResult.Error.Message,
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var (userId, email) = validationResult.Value;

        // Generate JWT tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(userId, email);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Set tokens in httpOnly cookies
        SetAuthCookies(accessToken, refreshToken);

        var response = ResponseMapper.ToLoginResponse(userId, email);
        return Ok(response);
    }

    /// <summary>
    /// Logs out the current user by clearing authentication cookies.
    /// </summary>
    /// <returns>No content</returns>
    /// <response code="200">Logout successful</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        ClearAuthCookies();
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Validates the current user session and returns user information.
    /// </summary>
    /// <returns>Current user information if session is valid</returns>
    /// <response code="200">Session valid, returns user info</response>
    /// <response code="401">Session invalid or expired</response>
    [HttpGet("session")]
    [Authorize]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult GetSession()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        if (userIdClaim is null || emailClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid Session",
                Detail = "Session claims are invalid",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var response = new SessionResponse(userId, emailClaim);
        return Ok(response);
    }

    /// <summary>
    /// Refreshes the access token using the refresh token from cookies.
    /// </summary>
    /// <returns>New tokens set in cookies</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="401">Refresh token invalid or expired</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Refresh()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Refresh token not found",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var accessToken = Request.Cookies["access_token"];
        if (string.IsNullOrEmpty(accessToken))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Access token not found",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // Validate existing token (even if expired, we can extract claims)
        var principal = _jwtTokenService.ValidateToken(accessToken);
        if (principal is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Invalid token",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // Extract user info
        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var emailClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        if (userIdClaim is null || emailClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "Invalid token claims",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        // Generate new tokens
        var newAccessToken = _jwtTokenService.GenerateAccessToken(userId, emailClaim);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        // Set new tokens in cookies
        SetAuthCookies(newAccessToken, newRefreshToken);

        var response = ResponseMapper.ToRefreshResponse(userId, emailClaim);
        return Ok(response);
    }

    #region Private Helper Methods

    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var accessTokenExpiration = jwtSettings.GetValue<int>("AccessTokenExpirationMinutes", 15);
        var refreshTokenExpiration = jwtSettings.GetValue<int>("RefreshTokenExpirationDays", 7);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None, // Required for cross-origin (different ports/protocols) TODO
            Path = "/"
        };

        // Access token cookie
        var accessCookieOptions = cookieOptions;
        accessCookieOptions.Expires = DateTimeOffset.UtcNow.AddMinutes(accessTokenExpiration);
        Response.Cookies.Append("access_token", accessToken, accessCookieOptions);

        // Refresh token cookie
        var refreshCookieOptions = cookieOptions;
        refreshCookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(refreshTokenExpiration);
        Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOptions);
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");
    }

    #endregion
}

