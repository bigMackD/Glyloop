using FluentValidation;

namespace Glyloop.Application.Queries.Chart.GetChartData;

/// <summary>
/// Validator for GetChartDataQuery.
/// Validates that range is a supported value.
/// </summary>
public class GetChartDataQueryValidator : AbstractValidator<GetChartDataQuery>
{
    private static readonly string[] ValidRanges = { "1h", "3h", "6h", "12h", "24h" };

    public GetChartDataQueryValidator()
    {
        RuleFor(x => x.Range)
            .NotEmpty()
            .WithMessage("Range is required.")
            .Must(BeValidRange)
            .WithMessage("Range must be one of: 1h, 3h, 6h, 12h, 24h.");
    }

    private bool BeValidRange(string range)
    {
        return ValidRanges.Contains(range?.ToLowerInvariant());
    }
}

