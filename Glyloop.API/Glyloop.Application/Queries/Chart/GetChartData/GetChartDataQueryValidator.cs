using System.Collections.Generic;
using FluentValidation;

namespace Glyloop.Application.Queries.Chart.GetChartData;

/// <summary>
/// Validator for GetChartDataQuery.
/// Validates that range is a supported value.
/// </summary>
public class GetChartDataQueryValidator : AbstractValidator<GetChartDataQuery>
{
    private static readonly HashSet<int> ValidRanges = new() { 1, 3, 5, 8, 12, 24 };

    public GetChartDataQueryValidator()
    {
        RuleFor(x => x.Range)
            .NotEmpty()
            .WithMessage("Range is required.")
            .Must(BeValidRange)
            .WithMessage("Range must be one of: 1, 3, 5, 8, 12, 24 (hours).");
    }

    private bool BeValidRange(string range)
    {
        return int.TryParse(range, out var hours) && ValidRanges.Contains(hours);
    }
}

