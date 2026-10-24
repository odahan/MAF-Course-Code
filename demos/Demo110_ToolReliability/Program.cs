/*
Code provided for private educational use only, without warranty.
See LICENCE.MD. Copyright Olivier Dahan 2026.
*/
using System.ComponentModel;
using System.Text.Json;
using Maf.Backlog;
using Maf.DemoConnection;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(2));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
var settings = DemoSettings.Load(args);
using var chat = DemoConnection.CreateChatClient(settings);
Console.WriteLine($"Provider: {settings.ChatProvider}");

Console.WriteLine("Simulated invoice fixture; no external billing system is contacted.");
var observed = new List<LookupResult>();
[Description("Looks up a simulated invoice by exact ID. INV-001 exists; other IDs return NotFound with no value. Read only.")]
LookupResult FindInvoice([Description("Exact invoice identifier, for example INV-001.")] string id)
{
    var result = LookupResult.SimulatedInvoice(id);
    observed.Add(result);
    return result;
}
foreach (var precise in new[] { false, true })
{
    observed.Clear();
    var tool = AIFunctionFactory.Create(FindInvoice, new AIFunctionFactoryOptions
    {
        Name = precise ? "find_invoice_by_exact_id" : "process",
        Description = precise
            ? "Reads the simulated invoice fixture by exact ID. INV-001 exists. Unknown IDs return NotFound and no value. Read only; cannot issue refunds."
            : "Processes a request."
    });
    var agent = chat.AsAIAgent(name: "ToolDescriptionComparison", instructions:
        "Use an available read tool for invoice INV-001. Answer in English, never claim a refund.", tools: [tool]);
    Console.WriteLine(precise ? "Precise catalogue:" : "Deliberately ambiguous catalogue (counterexample):");
    Console.WriteLine("Assistant declaration: " + (await agent.RunAsync("Read invoice INV-001.", cancellationToken: stop.Token)).Text);
    Console.WriteLine(observed.Count == 0 ? "No invocation observed; no verified value."
        : "Host answer: " + observed.Last().ToAnswer());
}
Console.WriteLine("Descriptions help tool selection; host permissions remain required regardless of description quality.");
