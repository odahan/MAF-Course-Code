using Maf.Observability;
using System.Text.Json;
using Maf.DemoConnection;
using Maf.Memory;
using Microsoft.Extensions.AI;

var settings = MemoryDemo.Load(args);
using var telemetry = Telemetry.CreateProvider(Telemetry.Print);
using var execution = Telemetry.Source.StartActivity("execution");
try
{
    var ledger = new UsageLedger();
    using var chat = new ObservedChatClient(DemoConnection.CreateChatClient(settings),
        usage => { ledger.Add(usage); Console.WriteLine("Usage per model request: " + JsonSerializer.Serialize(usage)); });
    using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(10));
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
    Console.WriteLine($"Provider: {settings.ChatProvider}. EP015 context size and provider-reported usage.");
    string corpus = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Donnees", "dossier_support_volumineux.txt"), stop.Token);
    string selected = string.Join('\n', corpus.Split('\n').Where(line =>
        line.StartsWith("Client :", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Situation :", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Impact :", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Contrainte", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Point métier", StringComparison.OrdinalIgnoreCase)
        || line.Contains("billing.outbound", StringComparison.OrdinalIgnoreCase)));
    if (string.IsNullOrWhiteSpace(selected))
        throw new InvalidDataException("The explicit scenario filter selected no lines. No fallback.");
    var agent = Telemetry.Instrument(chat.AsAIAgent(name: "SupportContextDiet", instructions: MemoryInstructions.Answer));
    foreach (var item in new[] { (Name: "raw-corpus", Text: corpus), (Name: "scenario-specific-line-filter", Text: selected) })
    {
        Console.WriteLine($"Policy={item.Name}; context characters={item.Text.Length}; character/4 estimate={(item.Text.Length + 3) / 4} tokens. Estimate excludes instructions and question.");
        var response = await agent.RunAsync(item.Text + "\nProvide a safe handover to support and mention missing information.", cancellationToken: stop.Token);
        Console.WriteLine(response.Text);
        MemoryDemo.PrintUsage(response);
    }
    Console.WriteLine("This line filter is deterministic selection for this fixture. Native MAF compaction and history policies follow.");
    await ContextExperiment.RunAsync(chat, stop.Token);
    Console.WriteLine("Experiment coverage including auxiliary calls: " + JsonSerializer.Serialize(ledger.Summarize()));
    Console.WriteLine("All extraction, summary and compaction calls are reported separately. No cost or savings claim; answer quality must be evaluated separately.");
    execution?.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
    return 0;
}
catch (Exception error) when (error is not OutOfMemoryException)
{
    execution?.SetStatus(System.Diagnostics.ActivityStatusCode.Error);
    Console.Error.WriteLine("Context experiment failed. No fallback or savings claim.");
    return 1;
}

