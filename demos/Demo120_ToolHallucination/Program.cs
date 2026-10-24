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
var agent = chat.AsAIAgent(name: "InvoiceReader", instructions:
    "Call find_invoice for INV-404. Never invent missing data. Answer in English.",
    tools: [AIFunctionFactory.Create(FindInvoice, "find_invoice")]);
Console.WriteLine("Assistant declaration: " + (await agent.RunAsync("Read invoice INV-404.", cancellationToken: stop.Token)).Text);
Console.WriteLine(observed.Count == 0 ? "No invocation observed; no verified answer."
    : "Host answer from observed tool result: " + observed.Last().ToAnswer());
