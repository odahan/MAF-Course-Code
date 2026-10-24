using System.Diagnostics;
using System.Text.Json;
using Maf.Backlog;
using Maf.DemoConnection;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Maf.Observability;

/// <summary>Qualifies EP016 routing separately from the common connection and reports triage overhead explicitly.</summary>
public static class RoutingExperiment
{
    /// <summary>Runs matched requests through a routed path and a direct baseline without claiming savings.</summary>
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Contains("--sensitive"))
        {
            Console.WriteLine("Host policy: human escalation. No model invoked; usage and cost unavailable.");
            return 0;
        }
        var settings = DemoSettings.Load(args);
        if (settings.ChatProvider != ConnectionProvider.Ollama)
            throw new ArgumentException("This optional routing experiment is qualified for Ollama only.");
        string triageModel = global::Maf.DemoConnection.DemoConfiguration.GetSetting("MAF_ROUTE_TRIAGE") ?? settings.LocalChat.Model;
        string easyModel = global::Maf.DemoConnection.DemoConfiguration.GetSetting("MAF_ROUTE_EASY") ?? settings.LocalChat.Model;
        string hardModel = global::Maf.DemoConnection.DemoConfiguration.GetSetting("MAF_ROUTE_HARD") ?? settings.LocalChat.Model;
        using var provider = Telemetry.CreateProvider(Telemetry.Print);
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var root = Telemetry.Source.StartActivity("execution");
        var routedUsage = new UsageLedger();
        var baselineUsage = new UsageLedger();
        Console.WriteLine("Fixed paired order: triage + routed execution, then direct hard-model baseline. Fresh sessions; same final prompt; warmup and quality are not controlled.");
        Console.WriteLine($"Models: triage={triageModel}; easy={easyModel}; hard={hardModel}. Identical profiles demonstrate mechanics only.");
        try
        {
            foreach (string request in new[] { "Rewrite professionally: the server is slow this morning.",
                "Analyze intermittent HTTP 503 after migration. List risks, hypotheses and checks; do not invent facts." })
            {
                var triage = await Call("triage", triageModel, "Classify the following request. Return exactly Easy or Hard.\n" + request);
                string route = triage.Trim();
                if (route is not ("Easy" or "Hard")) throw new InvalidDataException("Unrecognized triage result.");
                if (request.Contains("503", StringComparison.Ordinal)) route = "Hard";
                string prompt = "Answer briefly in English. Separate facts from hypotheses.\n" + request;
                _ = await Call("routed-execution", route == "Easy" ? easyModel : hardModel, prompt);
                _ = await Call("direct-hard-baseline", hardModel, prompt);
            }
            root?.SetStatus(ActivityStatusCode.Ok);
            Console.WriteLine("Routed path including triage: " + JsonSerializer.Serialize(routedUsage.Summarize()));
            Console.WriteLine("Direct baseline: " + JsonSerializer.Serialize(baselineUsage.Summarize()));
            Console.WriteLine("No cost, quality equivalence or savings claim. Include triage requests in any later comparison.");
            return 0;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            root?.SetStatus(ActivityStatusCode.Error);
            Console.Error.WriteLine("Routing experiment failed. No fallback answer or savings claim.");
            return 1;
        }

        async Task<string> Call(string stage, string model, string prompt)
        {
            using var client = new ObservedChatClient(DemoConnection.DemoConnection.CreateChatClient(settings with
                { LocalChat = settings.LocalChat with { Model = model } }),
                usage =>
                {
                    (stage == "direct-hard-baseline" ? baselineUsage : routedUsage).Add(usage);
                    Console.WriteLine(JsonSerializer.Serialize(new { Stage = stage, Model = model, Usage = usage }));
                });
            var agent = Telemetry.Instrument(client.AsAIAgent(name: "RoutingExperiment", instructions: "Follow the host task. No tools or business writes."));
            var timer = Stopwatch.StartNew();
            var response = await agent.RunAsync(prompt, cancellationToken: stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(new { Stage = stage, Model = model, DurationMs = timer.Elapsed.TotalMilliseconds }));
            return response.Text;
        }
    }
}
