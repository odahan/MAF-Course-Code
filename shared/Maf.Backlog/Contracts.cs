namespace Maf.Backlog;

/// <summary>Defines the same immutable draft at analysis, review and persistence boundaries.</summary>
public sealed record BacklogDraft(string Title, string Kind, string Priority, string Component,
    string Summary, string[] ClarifyingQuestions)
{
    /// <summary>Checks domains, required text, bounds and ambiguity without silently repairing data.</summary>
    public string[] Validate(bool forAction = true)
    {
        var errors = new List<string>();
        if (!Text(Title, 120)) errors.Add("Title must contain 1 to 120 characters.");
        if (!Text(Summary, 2000)) errors.Add("Summary must contain 1 to 2000 characters.");
        if (Kind is not ("Bug" or "Feature" or "Question")) errors.Add("Unknown kind.");
        if (Priority is not ("low" or "medium" or "high")) errors.Add("Unknown priority.");
        if (!Catalog.Names.Contains(Component)) errors.Add("Unknown component.");
        if (ClarifyingQuestions is null || ClarifyingQuestions.Length > 2
            || ClarifyingQuestions.Any(q => !Text(q, 300))) errors.Add("Invalid clarification questions.");
        if (forAction && ClarifyingQuestions is { Length: > 0 }) errors.Add("Clarification is required before writing.");
        return errors.ToArray();
    }

    private static bool Text(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max;
}

/// <summary>Provides the trusted component allowlist used at every write boundary.</summary>
public static class Catalog
{
    /// <summary>Gets supported component identifiers; source documents cannot extend this allowlist.</summary>
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(
        new[] { "UI", "API", "Auth", "Billing", "Reporting", "Storage", "Other" });
}

/// <summary>Distinguishes an observed invocation result from generated assistant text.</summary>
public sealed record ToolOutcome(string Status, long? ItemId, string Detail);

/// <summary>Represents a stored row read back from SQLite.</summary>
public sealed record BacklogItem(long Id, string Title, string Kind, string Priority, string Component, string Summary);
