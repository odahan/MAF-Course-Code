using System.Text.Json;
using Episode011.ModeDegrade;
using Maf.Backlog;

var scenario = args.Contains("--live") ? ScenarioDemo.Nominal
    : args.Contains("--timeout") ? ScenarioDemo.TimeoutSimule
    : args.Contains("--invalid") ? ScenarioDemo.SortieInvalideSimulee
    : args.Contains("--unavailable") ? ScenarioDemo.OllamaIndisponibleSimule : ScenarioDemo.FallbackDirect;
using var caller = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; caller.Cancel(); };
var options = DemoSettings.Load(args.Where(a => a is not ("--live" or "--timeout" or "--invalid" or "--unavailable")).ToArray());
var service = new ResilientAgentTriageService(options);
try
{
    var result = await service.ClasserAsync("Production login is blocked for a known account.", scenario, caller.Token);
    Console.WriteLine(JsonSerializer.Serialize(result));
    return 0;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Canceled by caller; no heuristic result produced.");
    return 130;
}
