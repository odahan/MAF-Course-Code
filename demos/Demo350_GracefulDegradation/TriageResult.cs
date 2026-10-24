namespace Episode011.ModeDegrade;

/// <summary>Reports provenance and degradation; confidence is null unless independently calibrated.</summary>
public sealed record TriageResult(string Categorie, string Decision, double? Confiance,
    string Explication, string Origine, string? CauseBascule);
