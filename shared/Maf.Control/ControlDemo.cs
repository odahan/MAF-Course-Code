using System.Text.Json;
using Maf.Backlog;
using Maf.DemoConnection;
using Microsoft.Extensions.AI;

namespace Maf.Control;

/// <summary>Runs the shared middleware and Harness acceptance scenarios.</summary>
public static class ControlDemo
{
    /// <summary>Uses a scripted transport by default and the common model connection on explicit selection.</summary>
    public static async Task<int> RunAsync(string[] args, string defaultScenario = "complete", bool harness = true)
    {
        var live = args.Contains("--live");
        string[] scenarios = ["complete", "repeat", "forbidden", "budget", "incomplete", "deadline", "changing"];
        var scenario = args.FirstOrDefault(a => scenarios.Contains(a)) ?? defaultScenario;
        using var caller = new CancellationTokenSource();
        var capabilities = scenario == "forbidden" ? new[] { "review.finish" } : new[] { "ticket.read", "review.finish" };
        var limits = new ExecutionLimits(ModelCalls: scenario == "budget" ? 1 : 6,
            DeadlineSeconds: scenario == "deadline" ? 1 : 120);
        using var scope = new ExecutionScope(capabilities, limits, caller.Token);
        using IChatClient client = live ? DemoConnection.DemoConnection.CreateChatClient(DemoSettings.Load(
            args.Where(a => a != "--live" && a != scenario && a != "--simple").ToArray())) : new ScriptedReviewClient(scenario);
        var task = new ReviewTask();
        var useHarness = harness && !args.Contains("--simple");
        var agent = BoundedAgent.Create(client, scope, task.CreateTools(scope), harness: useHarness);
        Console.WriteLine(JsonSerializer.Serialize(new { Agent = useHarness ? "Native HarnessAgent" : "Native ChatClientAgent",
            Runtime = agent.GetType().Name, Transport = live ? "Configured model" : "Scripted fixture",
            capabilities, limits, EndCondition = "Read ticket 42 and finish review; no business write." }));
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; caller.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            var result = await BoundedAgent.RunAsync(agent, scope, "Review ticket 42 using both tools in order.", () => task.Completed, caller.Token);
            Console.WriteLine(JsonSerializer.Serialize(result));
            return result.Status == "Completed" ? 0 : 2;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Canceled by caller.");
            return 130;
        }
        catch (HttpRequestException)
        {
            Console.WriteLine(JsonSerializer.Serialize(new RunOutcome("ProviderUnavailable", null, scope.Events)));
            return 2;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }
}

