using Glyloop.API.Contracts.Dexcom;
using Glyloop.API.Mapping;
using Glyloop.Application.Common.Interfaces;
using Glyloop.Application.Queries.DexcomLink.GetDexcomLinkStatus;
using Glyloop.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glyloop.API.Controllers;

/// <summary>
/// Manages Dexcom CGM integration via OAuth.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DexcomController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;

    public DexcomController(
        IMediator mediator,
        ICurrentUserService currentUserService,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _configuration = configuration;
    }

    /// <summary>
    /// Initiates Dexcom OAuth authorization flow by redirecting to Dexcom.
    /// </summary>
    /// <returns>Redirect to Dexcom OAuth authorization page</returns>
    /// <response code="302">Redirect to Dexcom authorization</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult Authorize()
    {
        var dexcomSettings = _configuration.GetSection("Dexcom");
        var clientId = dexcomSettings["ClientId"];
        var redirectUri = dexcomSettings["RedirectUri"];
        var baseUrl = dexcomSettings["BaseUrl"] ?? "https://sandbox-api.dexcom.com";

        var authUrl = $"{baseUrl}/v2/oauth2/login" +
            $"?client_id={clientId}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}" +
            $"&response_type=code" +
            $"&scope=offline_access" +
            $"&state={_currentUserService.UserId}";

        return Redirect(authUrl);
    }

    /// <summary>
    /// Handles OAuth callback from Dexcom with authorization code.
    /// This endpoint is typically called by Dexcom, not directly by the client.
    /// </summary>
    /// <param name="code">Authorization code from Dexcom</param>
    /// <param name="state">User ID passed in authorize request</param>
    /// <returns>Redirect to frontend with success/error</returns>
    /// <response code="302">Redirect to frontend</response>
    [HttpGet("callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Callback([FromQuery] string code, [FromQuery] string state)
    {
        var frontendUrl = _configuration["CorsSettings:AllowedOrigins:0"] ?? "http://localhost:4200";

        if (string.IsNullOrEmpty(code))
        {
            return Redirect($"{frontendUrl}/dexcom-link?error=no_code");
        }

        // Redirect to frontend with code, frontend will call /api/dexcom/link
        return Redirect($"{frontendUrl}/dexcom-link?code={code}");
    }

    /// <summary>
    /// Links Dexcom account by exchanging authorization code for access tokens.
    /// </summary>
    /// <param name="request">Authorization code from OAuth flow</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Link status and expiration</returns>
    /// <response code="201">Dexcom account linked successfully</response>
    /// <response code="400">Invalid authorization code</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("link")]
    [ProducesResponseType(typeof(LinkDexcomResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LinkDexcomResponse>> Link(
        [FromBody] LinkDexcomRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Link Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var response = result.Value.ToResponse();
        return CreatedAtAction(nameof(GetStatus), null, response);
    }

    /// <summary>
    /// Unlinks the current user's Dexcom account.
    /// Stops new data imports but retains existing glucose data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="200">Dexcom account unlinked successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">No Dexcom link found</response>
    [HttpDelete("unlink")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlink(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var command = userId.ToCommand();
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "DexcomLink.LinkNotFound")
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Link Not Found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound
                });
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Unlink Failed",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(new { message = "Dexcom account unlinked successfully" });
    }

    /// <summary>
    /// Gets the current status of Dexcom integration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dexcom link status</returns>
    /// <response code="200">Status retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(DexcomStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DexcomStatusResponse>> GetStatus(
        CancellationToken cancellationToken)
    {
        var query = new GetDexcomLinkStatusQuery();
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            // Return empty status if no link found
            return Ok(new DexcomStatusResponse(
                IsLinked: false,
                LinkedAt: null,
                TokenExpiresAt: null,
                LastSyncAt: null));
        }

        var response = result.Value.ToResponse();
        return Ok(response);
    }
}

