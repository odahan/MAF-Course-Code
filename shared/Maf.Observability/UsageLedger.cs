namespace Maf.Observability;

/// <summary>Summarizes known subtotals without presenting incomplete coverage as an execution total.</summary>
public sealed record UsageSummary(int Requests, int RequestsWithInput, int RequestsWithOutput,
    long KnownInputSubtotal, long KnownOutputSubtotal, long? CompleteInputTotal, long? CompleteOutputTotal);

/// <summary>Collects request-level counters for one experiment, including auxiliary model requests.</summary>
public sealed class UsageLedger
{
    private readonly List<Consumption> requests = [];

    /// <summary>Records exactly one transport-request report.</summary>
    public void Add(Consumption consumption)
    {
        lock (requests) requests.Add(consumption);
    }

    /// <summary>Only exposes totals when at least one request was made and every relevant counter is known.</summary>
    public UsageSummary Summarize()
    {
        lock (requests)
        {
            int inputs = requests.Count(r => r.Input.HasValue);
            int outputs = requests.Count(r => r.Output.HasValue);
            long knownInput = requests.Where(r => r.Input.HasValue).Sum(r => r.Input!.Value);
            long knownOutput = requests.Where(r => r.Output.HasValue).Sum(r => r.Output!.Value);
            return new(requests.Count, inputs, outputs, knownInput, knownOutput,
                inputs == requests.Count && inputs > 0 ? knownInput : null,
                outputs == requests.Count && outputs > 0 ? knownOutput : null);
        }
    }
}
