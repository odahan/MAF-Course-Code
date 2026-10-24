using System.Text.Json;
using Episode011.ModeDegrade;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.Control.Tests;

/// <summary>Exercises actual native loops, guarded direct invocations and cancellation precedence.</summary>
[TestClass]
public sealed class ControlTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TwoStepTaskCompletesOnBothNativeAgents(bool harness)
    {
        using var scope = new ExecutionScope(["ticket.read", "review.finish"]);
        using var client = new ScriptedReviewClient();
        var task = new ReviewTask();
        var agent = BoundedAgent.Create(client, scope, task.CreateTools(scope), harness);
        var result = await BoundedAgent.RunAsync(agent, scope, "Review 42", () => task.Completed);
        Assert.AreEqual("Completed", result.Status);
        Assert.AreEqual(1, task.Reads);
        Assert.AreEqual(3, client.Calls);
        Assert.AreEqual(2, result.Events.Count(e => e.Kind == "ToolCall"));
        Assert.AreEqual(2, result.Events.Count(e => e.Kind == "Middleware"));
    }

    [TestMethod]
    [DataRow("repeat", "RepeatedCall", 1)]
    [DataRow("forbidden", "PermissionDenied", 0)]
    [DataRow("budget", "ModelBudget", 1)]
    [DataRow("incomplete", "Incomplete", 0)]
    [DataRow("deadline", "DeadlineExceeded", 0)]
    [DataRow("changing", "ToolBudget", 0)]
    public async Task RealHarnessLoopHasExplicitTerminalState(string scenario, string status, int reads)
    {
        using var scope = new ExecutionScope(scenario == "forbidden" ? [] : ["ticket.read", "review.finish"],
            new(ModelCalls: scenario == "budget" ? 1 : 6, DeadlineSeconds: scenario == "deadline" ? 1 : 10));
        using var client = new ScriptedReviewClient(scenario);
        var task = new ReviewTask();
        var agent = BoundedAgent.Create(client, scope, task.CreateTools(scope));
        var result = await BoundedAgent.RunAsync(agent, scope, "Review 42", () => task.Completed);
        Assert.AreEqual(status, result.Status);
        Assert.AreEqual(reads, task.Reads);
        Assert.IsFalse(task.Completed);
        Assert.IsTrue(client.Calls <= 6);
    }

    [TestMethod]
    public async Task DirectInvocationCannotBypassPermission()
    {
        using var scope = new ExecutionScope([]);
        var task = new ReviewTask();
        var tool = (AIFunction)task.CreateTools(scope)[0];
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await tool.InvokeAsync(new() { ["input"] = 42 }));
        Assert.AreEqual(0, task.Reads);
        Assert.AreEqual("PermissionDenied", scope.StopReason);
    }

    [TestMethod]
    public async Task TypedJsonAndClrInputsShareRepetitionKey()
    {
        using var scope = new ExecutionScope(["ticket.read"]);
        var task = new ReviewTask();
        var tool = (AIFunction)task.CreateTools(scope)[0];
        await tool.InvokeAsync(new() { ["input"] = JsonSerializer.SerializeToElement(42) });
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await tool.InvokeAsync(new() { ["input"] = 42 }));
        Assert.AreEqual(1, task.Reads);
    }

    [TestMethod]
    public async Task ObjectPropertyOrderAndDecimalScaleAreNormalized()
    {
        using var scope = new ExecutionScope(["read"]);
        var bodies = 0;
        var tool = scope.CreateTool<Dictionary<string, decimal>>("lookup", "read", (_, _) =>
        {
            bodies++;
            return Task.FromResult("found");
        });
        await tool.InvokeAsync(new() { ["input"] = JsonSerializer.Deserialize<JsonElement>("{\"b\":2.0,\"a\":1}") });
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await tool.InvokeAsync(
            new() { ["input"] = JsonSerializer.Deserialize<JsonElement>("{\"a\":1.00,\"b\":2}") }));
        Assert.AreEqual(1, bodies);
    }

    [TestMethod]
    public async Task DistinctTypedValuesConsumeToolBudget()
    {
        using var scope = new ExecutionScope(["read"], new(ToolCalls: 2));
        var bodies = 0;
        var tool = scope.CreateTool<int>("lookup", "read", (_, _) => Task.FromResult((++bodies).ToString()));
        await tool.InvokeAsync(new() { ["input"] = 1 });
        await tool.InvokeAsync(new() { ["input"] = 2 });
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await tool.InvokeAsync(new() { ["input"] = 3 }));
        Assert.AreEqual(2, bodies);
        Assert.AreEqual("ToolBudget", scope.StopReason);
    }

    [TestMethod]
    public async Task FreshScopesDoNotPersistLoopHistory()
    {
        for (var i = 0; i < 2; i++)
        {
            using var scope = new ExecutionScope(["ticket.read"]);
            var task = new ReviewTask();
            await ((AIFunction)task.CreateTools(scope)[0]).InvokeAsync(new() { ["input"] = 42 });
            Assert.AreEqual(1, task.Reads);
        }
    }

    [TestMethod]
    public async Task CancellationReachesToolBody()
    {
        using var caller = new CancellationTokenSource();
        using var scope = new ExecutionScope(["wait"], cancellationToken: caller.Token);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tool = scope.CreateTool<int>("wait", "wait", async (_, token) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return "never";
        });
        var pending = tool.InvokeAsync(new() { ["input"] = 1 }).AsTask();
        await started.Task;
        caller.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => pending);
    }

    [TestMethod]
    public async Task CallerCancellationThroughNativeHarnessPropagates()
    {
        using var caller = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        using var scope = new ExecutionScope(["ticket.read"], cancellationToken: caller.Token);
        using var client = new ScriptedReviewClient("deadline");
        var task = new ReviewTask();
        var agent = BoundedAgent.Create(client, scope, task.CreateTools(scope));
        await Assert.ThrowsAsync<OperationCanceledException>(() => BoundedAgent.RunAsync(agent, scope, "Review", () => task.Completed));
    }

    [TestMethod]
    public async Task ConcurrentDuplicateCallsReachBodyOnce()
    {
        using var scope = new ExecutionScope(["read"]);
        var bodies = 0;
        var tool = scope.CreateTool<int>("read", "read", (_, _) => Task.FromResult(Interlocked.Increment(ref bodies).ToString()));
        async Task Invoke()
        {
            try { await tool.InvokeAsync(new() { ["input"] = 42 }); }
            catch (OperationCanceledException) { }
        }
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(Invoke)));
        Assert.AreEqual(1, bodies);
    }

    [TestMethod]
    [DataRow(ScenarioDemo.FallbackDirect)]
    [DataRow(ScenarioDemo.OllamaIndisponibleSimule)]
    [DataRow(ScenarioDemo.SortieInvalideSimulee)]
    public async Task DegradationIsExplicitAndNotMeasuredConfidence(ScenarioDemo scenario)
    {
        var service = new ResilientAgentTriageService();
        var result = await service.ClasserAsync("login", scenario, CancellationToken.None);
        Assert.AreEqual("Heuristic", result.Origine);
        Assert.IsNull(result.Confiance);
        Assert.IsNotNull(result.CauseBascule);
    }

    [TestMethod]
    public async Task ModelTimeoutDegradesWithoutProviderSwitch()
    {
        var calls = 0;
        var service = new ResilientAgentTriageService(model: async (_, token) =>
        {
            calls++;
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new InvalidOperationException();
        }, timeout: TimeSpan.FromMilliseconds(100));
        var result = await service.ClasserAsync("login", ScenarioDemo.Nominal, CancellationToken.None);
        Assert.AreEqual("Heuristic", result.Origine);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public async Task Ep011CancellationCannotBecomeFallback()
    {
        using var caller = new CancellationTokenSource();
        var service = new ResilientAgentTriageService(model: (_, _) =>
        {
            caller.Cancel();
            throw new HttpRequestException("Failure racing with cancellation.");
        });
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ClasserAsync("login", ScenarioDemo.Nominal, caller.Token));
    }

    [TestMethod]
    public async Task PreCanceledExplicitFallbackStillPropagatesCancellation()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => new ResilientAgentTriageService()
            .ClasserAsync("login", ScenarioDemo.FallbackDirect, caller.Token));
    }

    [TestMethod]
    public async Task Ep011CancellationDuringModelCallPropagates()
    {
        using var caller = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var service = new ResilientAgentTriageService(model: async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            throw new InvalidOperationException();
        });
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ClasserAsync("login", ScenarioDemo.Nominal, caller.Token));
    }

    [TestMethod]
    public async Task NormalModelResultDoesNotInventMeasuredConfidence()
    {
        var service = new ResilientAgentTriageService(model: (_, _) => Task.FromResult(
            new TriageResult("Access", "Review account access.", 0.99, "Model claim", "Model", null)));
        var result = await service.ClasserAsync("login", ScenarioDemo.Nominal, CancellationToken.None);
        Assert.AreEqual("Model", result.Origine);
        Assert.IsNull(result.Confiance);
    }

    [TestMethod]
    public async Task StringCaseAndArrayOrderRemainSignificant()
    {
        using var scope = new ExecutionScope(["read"]);
        var count = 0;
        var tool = scope.CreateTool<string[]>("read", "read", (_, _) => Task.FromResult((++count).ToString()));
        await tool.InvokeAsync(new() { ["input"] = new[] { "A", "b" } });
        await tool.InvokeAsync(new() { ["input"] = new[] { "a", "b" } });
        await tool.InvokeAsync(new() { ["input"] = new[] { "b", "A" } });
        Assert.AreEqual(3, count);
    }
}
