using Microsoft.Extensions.AI;

namespace Maf.Observability;

/// <summary>Keeps reported, derived, estimated and monetary quantities separate.</summary>
public sealed record Consumption(long? Input, long? Output, long? ReportedTotal, long? DerivedTotal,
    long? CharacterEstimate, decimal? EstimatedCost, string? Currency, string? RateVersion)
{
    /// <summary>Never substitutes missing usage with zero or a character estimate. Rates are caller-supplied, not invoices.</summary>
    public static Consumption Measure(UsageDetails? usage, int? contextCharacters = null, TokenRate? rate = null)
    {
        if (contextCharacters < 0) throw new ArgumentOutOfRangeException(nameof(contextCharacters));
        if (usage?.InputTokenCount < 0 || usage?.OutputTokenCount < 0 || usage?.TotalTokenCount < 0)
            throw new ArgumentException("Token counts cannot be negative.");
        rate?.Validate();
        var input = usage?.InputTokenCount;
        var output = usage?.OutputTokenCount;
        long? derived = input.HasValue && output.HasValue ? checked(input.Value + output.Value) : null;
        decimal? cost = rate is not null && input.HasValue && output.HasValue
            ? (input.Value * rate.InputPerMillion + output.Value * rate.OutputPerMillion) / 1_000_000m : null;
        return new(input, output, usage?.TotalTokenCount, derived,
            contextCharacters.HasValue ? ((long)contextCharacters.Value + 3) / 4 : null,
            cost, rate?.Currency, rate?.Version);
    }
}

/// <summary>Describes an explicit dated price assumption for one provider/model; no rate is supplied by default.</summary>
public sealed record TokenRate(decimal InputPerMillion, decimal OutputPerMillion, string Currency, string Version)
{
    /// <summary>Rejects negative or unattributed price assumptions.</summary>
    public void Validate()
    {
        if (InputPerMillion < 0 || OutputPerMillion < 0 || string.IsNullOrWhiteSpace(Currency) || string.IsNullOrWhiteSpace(Version))
            throw new ArgumentException("Rates require nonnegative prices, currency and a version.");
    }
}
