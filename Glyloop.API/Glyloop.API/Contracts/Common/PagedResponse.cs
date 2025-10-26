namespace Glyloop.API.Contracts.Common;

/// <summary>
/// Generic paged response wrapper for list endpoints.
/// </summary>
/// <typeparam name="T">The type of items in the page</typeparam>
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

