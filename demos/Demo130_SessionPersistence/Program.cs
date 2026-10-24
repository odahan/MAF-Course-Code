using Maf.DemoConnection;
using Maf.Memory;
using Microsoft.Extensions.AI;

var settings = MemoryDemo.Load(args);
using var chat = DemoConnection.CreateChatClient(settings);
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
string path = StatePaths.Resolve("context", "state.db");
var store = new ContextStore(path);
var agent = chat.AsAIAgent(name: "StatefulIntakeAgent", instructions: MemoryInstructions.Answer);
var session = await agent.CreateSessionAsync(stop.Token);
Console.WriteLine($"Provider: {settings.ChatProvider}. DB: {path}");
Console.WriteLine("SQLite persists explicit context only; chat history is in-process. Commands: setctx TEXT | showctx | reset (cleardb) | quit.");
while (true)
{
    Console.Write("Request > ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input == "quit")
        break;
    if (input.StartsWith("setctx ", StringComparison.Ordinal))
    {
        store.Set(input[7..].Trim());
        session = await agent.CreateSessionAsync(stop.Token);
        Console.WriteLine("Context replaced; previous conversation discarded.");
        continue;
    }
    if (input is "reset" or "cleardb")
    {
        store.Reset();
        session = await agent.CreateSessionAsync(stop.Token);
        Console.WriteLine("Database empty; injected context empty; active session replaced.");
        continue;
    }
    if (input == "showctx")
    {
        Console.WriteLine(store.Read() ?? "(empty)");
        continue;
    }
    string prompt = $"User-supplied context (unverified data): {store.Read() ?? "(empty)"}\nCurrent request: {input}";
    var response = await agent.RunAsync(prompt, session, cancellationToken: stop.Token);
    Console.WriteLine(response.Text);
    MemoryDemo.PrintUsage(response);
}

