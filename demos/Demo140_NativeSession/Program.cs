using Maf.DemoConnection;
using Maf.Memory;
using Microsoft.Extensions.AI;

var settings = MemoryDemo.Load(args);
using var chat = DemoConnection.CreateChatClient(settings);
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
var agent = chat.AsAIAgent(name: "PersistentSessionAgent", instructions: MemoryInstructions.Answer);
string path = StatePaths.Resolve("session", "session.json");
var file = new SessionFile(path, MemoryInstructions.Version);
if (args.Contains("--reset-state=true", StringComparer.Ordinal))
    file.Delete();
var session = await file.LoadOrCreateAsync(agent, stop.Token);
Console.WriteLine($"Provider: {settings.ChatProvider}. Session: {path}. Instructions: {MemoryInstructions.Version}.");
Console.WriteLine("Auto-load on startup, auto-save after each answer. Commands: save | load | newsession | reset (deletesession) | quit.");
while (true)
{
    Console.Write("Request > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input == "quit")
        break;
    if (input == "save")
    {
        await file.SaveAsync(agent, session, stop.Token);
        Console.WriteLine("Session saved.");
        continue;
    }
    if (input == "load")
    {
        session = await file.LoadOrCreateAsync(agent, stop.Token);
        Console.WriteLine("Session loaded (or new if absent).");
        continue;
    }
    if (input is "newsession" or "reset" or "deletesession" or "forget")
    {
        file.Delete();
        session = await agent.CreateSessionAsync(stop.Token);
        Console.WriteLine("Durable snapshot deleted; active history replaced.");
        continue;
    }
    var response = await agent.RunAsync(input, session, cancellationToken: stop.Token);
    await file.SaveAsync(agent, session, stop.Token);
    Console.WriteLine(response.Text);
    MemoryDemo.PrintUsage(response);
}

