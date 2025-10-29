using Glyloop.Application.DTOs.Chart;
using Glyloop.Domain.Common;
using MediatR;

namespace Glyloop.Application.Queries.Chart.GetChartData;

/// <summary>
/// Query to retrieve glucose data and event overlays for chart display.
/// Supports various time ranges (1, 3, 5, 8, 12, 24 hours).
/// </summary>
public record GetChartDataQuery(
    string Range) : IRequest<Result<ChartDataDto>>;

