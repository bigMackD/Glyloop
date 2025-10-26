using Glyloop.Application.DTOs.Chart;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Queries.Chart.GetChartData;

/// <summary>
/// Query to retrieve glucose data and event overlays for chart display.
/// Supports various time ranges (1h, 3h, 6h, 12h, 24h).
/// </summary>
public record GetChartDataQuery(
    string Range) : IRequest<Result<ChartDataDto>>;

