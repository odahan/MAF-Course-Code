using System.Text.Json;
using LocalMcp;
using Maf.Backlog;
using Maf.DemoConnection;
using Microsoft.Extensions.AI;

using var caller = new CancellationTokenSource();
ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; caller.Cancel(); };
Console.CancelKeyPress += handler;
try
{
    var serverIndex = Array.IndexOf(args, "--server");
    if (serverIndex >= 0 && (serverIndex + 1 >= args.Length || args[serverIndex + 1].StartsWith("--", StringComparison.Ordinal)))
        throw new ArgumentException("The --server option requires a compiled server DLL path.");
    var server = serverIndex >= 0 && serverIndex + 1 < args.Length ? args[serverIndex + 1] : LocalMcpSession.DefaultServerPath;
    await using var session = await LocalMcpSession.ConnectAsync(server, TimeSpan.FromSeconds(15), caller.Token);
    Console.WriteLine(JsonSerializer.Serialize(new { Discovered = session.Tools.Select(t => new { t.Name, t.JsonSchema }) }));
    if (!args.Contains("--live"))
    {
        foreach (var id in new[] { "INC-318", "INC-999" })
        {
            var result = await session.CallAsync("get_incident_status", new() { ["incidentId"] = id }, caller.Token);
            if (result.IsError == true) throw new InvalidOperationException("MCP tool failed.");
            Console.WriteLine(JsonSerializer.Serialize(new { Tool = "get_incident_status", IncidentId = id, Result = result }));
        }
        var customers = await session.CallAsync("list_priority_customers", new(), caller.Token);
        if (customers.IsError == true) throw new InvalidOperationException("MCP tool failed.");
        Console.WriteLine(JsonSerializer.Serialize(new { Tool = "list_priority_customers", Result = customers }));
        Console.WriteLine(JsonSerializer.Serialize(new { Status = "Completed", Mode = "Protocol" }));
        return 0;
    }

    var configArgs = args.Where((_, i) => args[i] != "--live" && i != serverIndex && (serverIndex < 0 || i != serverIndex + 1)).ToArray();
    using IChatClient model = DemoConnection.CreateChatClient(DemoSettings.Load(configArgs));
    var outcome = await session.RunAgentAsync(model, "List priority customers, then get incident INC-318 and incident INC-999.", caller.Token);
    Console.WriteLine(JsonSerializer.Serialize(outcome));
    return outcome.Status == "Completed" ? 0 : 2;
}
catch (OperationCanceledException)
{
    Console.WriteLine(JsonSerializer.Serialize(new { Status = caller.IsCancellationRequested ? "Canceled" : "DeadlineExceeded" }));
    return caller.IsCancellationRequested ? 130 : 2;
}
catch (Exception error)
{
    Console.Error.WriteLine($"MCP demo failed: {error.GetType().Name}. Check the compiled server and selected provider.");
    Console.WriteLine(JsonSerializer.Serialize(new { Status = "Failed", Error = error.GetType().Name }));
    return 2;
}
finally { Console.CancelKeyPress -= handler; }
