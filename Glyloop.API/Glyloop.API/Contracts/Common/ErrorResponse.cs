namespace Glyloop.API.Contracts.Common;

/// <summary>
/// Standard error response for API errors.
/// </summary>
public record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string? Detail,
    string? TraceId,
    Dictionary<string, string[]>? Errors = null);

