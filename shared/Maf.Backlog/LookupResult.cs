namespace Maf.Backlog;

/// <summary>Represents an authoritative lookup, including absent data and tool failures.</summary>
public sealed record LookupResult(string Status, string? Value)
{
    /// <summary>Reads a deliberately simulated invoice fixture; never presents missing data as a value.</summary>
    public static LookupResult SimulatedInvoice(string id) => id == "INV-001"
        ? new("Found", "42 EUR") : new("NotFound", null);

    /// <summary>Builds the factual answer from observed tool data rather than generated claims.</summary>
    public string ToAnswer() => Status switch
    {
        "Found" when !string.IsNullOrWhiteSpace(Value) => Value,
        "NotFound" => "No matching data exists in the demo fixture.",
        "Refused" => "The host refused this lookup.",
        _ => "The lookup failed; no factual value is available."
    };
}
