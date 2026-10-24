using Maf.Backlog;

var draft = new BacklogDraft("CSV export", "Bug", "high", "Reporting",
    "Large CSV exports freeze the UI.", ["Which dataset size reproduces the issue?"]);
int analysisErrors = draft.Validate(forAction: false).Length;
int actionErrors = draft.Validate(forAction: true).Length;
var clarified = draft with { ClarifyingQuestions = [] };
int clarifiedErrors = clarified.Validate(forAction: true).Length;
Console.WriteLine($"Analysis errors: {analysisErrors}");
Console.WriteLine($"Action errors before clarification: {actionErrors}");
Console.WriteLine($"Action errors after clarification: {clarifiedErrors}");
Console.WriteLine("Validation does not grant permission or approval. No business write was attempted.");
return analysisErrors == 0 && actionErrors == 1 && clarifiedErrors == 0 ? 0 : 1;
