using System.Text.Json;
using Maf.Backlog;
using Maf.DemoConnection;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var settings = DemoSettings.Load(args);
using var chat = DemoConnection.CreateChatClient(settings);
using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(5));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
Console.WriteLine($"Provider: {settings.ChatProvider}. Empty input exits.");

var agent = chat.AsAIAgent(name: "IntakeAgent", instructions:
    "Classify a product request as Bug, Feature or Question. Suggest priority and component. Answer in English. No actions are available.");
while (true)
{
    Console.Write("Request > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;
    Console.WriteLine((await agent.RunAsync(input, cancellationToken: stop.Token)).Text);
}
