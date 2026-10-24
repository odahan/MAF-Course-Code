namespace Episode011.ModeDegrade;

/// <summary>Offers a deliberately limited keyword heuristic, never a measured probability.</summary>
public sealed class DeterministicFallbackClassifier
{
    /// <summary>Returns an explicitly degraded result with no numeric confidence.</summary>
    public TriageResult Classer(string request, string reason)
    {
        var text = request.ToLowerInvariant();
        var category = text.Contains("invoice") || text.Contains("payment") ? "Billing"
            : text.Contains("password") || text.Contains("login") || text.Contains("account") ? "Access"
            : text.Contains("urgent") || text.Contains("production") ? "Incident" : "General";
        return new(category, "Create a support request for human review.", null,
            "Degraded mode: keyword heuristic only; confidence has not been measured.", "Heuristic", reason);
    }
}
