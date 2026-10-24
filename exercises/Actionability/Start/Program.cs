using Maf.Backlog;

var draft = new BacklogDraft("CSV export", "Bug", "high", "Reporting",
    "Large CSV exports freeze the UI.", ["Which dataset size reproduces the issue?"]);
Console.WriteLine("Exercise incomplete: implement the read-only analysis/action comparison.");
Console.WriteLine($"Analysis errors: {draft.Validate(forAction: false).Length}");
return 2;
