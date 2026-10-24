using System.Text.Json;
using System.Diagnostics;
using Maf.Backlog;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.Workflows.Tests;

/// <summary>Exercises actual MAF graphs and the native agent binding with a scripted transport.</summary>
[TestClass]
public sealed class WorkflowTests
{
    [TestMethod]
    public async Task TelemetryHasTraceSpansDistinctFromBusinessOutput()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.Agents.AI.Workflows",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => spans.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        var events = await Execute(BacklogWorkflow.Build(telemetry: true), BacklogWorkflow.Example);
        Assert.AreEqual("Ready", Result(events).Status);
        Assert.IsTrue(spans.Count > 0);
        Assert.IsTrue(spans.Any(s => s.ParentSpanId != default));
        Assert.IsFalse(spans.SelectMany(s => s.TagObjects).Any(t => t.Value?.ToString()?.Contains("Login returns 401", StringComparison.Ordinal) == true));
    }

    [TestMethod]
    public async Task DeterministicReadyTakesExactlyOneBranch()
    {
        var events = await Execute(BacklogWorkflow.Build(), BacklogWorkflow.Example);
        CollectionAssert.AreEqual(new[] { "AnalyzeDraft", "AcceptDraft" }, Path(events));
        Assert.AreEqual("Ready", Result(events).Status);
    }

    [TestMethod]
    public async Task ClarificationSkipsNativeAgentEntirely()
    {
        using var client = new ScriptedClient();
        var events = await Execute(BacklogWorkflow.Build(BacklogWorkflow.CreateReviewer(client)),
            BacklogWorkflow.Example with { ClarifyingQuestions = ["Which account?"] });
        CollectionAssert.AreEqual(new[] { "AnalyzeDraft", "ClarifyDraft" }, Path(events));
        Assert.AreEqual("NeedsClarification", Result(events).Status);
        Assert.AreEqual(0, client.Calls);
    }

    [TestMethod]
    [DataRow("kind")]
    [DataRow("component")]
    [DataRow("summary")]
    public async Task InvalidInputCannotReachReadyBranch(string field)
    {
        var draft = field switch
        {
            "kind" => BacklogWorkflow.Example with { Kind = "Delete" },
            "component" => BacklogWorkflow.Example with { Component = "Unknown" },
            _ => BacklogWorkflow.Example with { Summary = "" }
        };
        var events = await Execute(BacklogWorkflow.Build(), draft);
        CollectionAssert.AreEqual(new[] { "AnalyzeDraft", "ClarifyDraft" }, Path(events));
        Assert.AreEqual("NeedsClarification", Result(events).Status);
    }

    [TestMethod]
    public async Task NativeAgentRunsOnceWithSchemaAndHostPostValidation()
    {
        using var client = new ScriptedClient();
        var events = await Execute(BacklogWorkflow.Build(BacklogWorkflow.CreateReviewer(client)), BacklogWorkflow.Example);
        var path = Path(events);
        Assert.AreEqual(5, path.Length, string.Join(" -> ", path));
        Assert.AreEqual("AnalyzeDraft", path[0]);
        Assert.AreEqual("PrepareReview", path[1]);
        Assert.IsTrue(path[2].StartsWith("BacklogReviewer_", StringComparison.Ordinal));
        Assert.AreEqual(path[2], path[3]);
        Assert.AreEqual("ValidateReview", path[4]);
        Assert.AreEqual(1, client.Calls);
        Assert.IsTrue(client.SawSchema);
        Assert.IsTrue(events.OfType<AgentResponseEvent>().Any());
        Assert.AreEqual("Reviewed", Result(events).Status);
        Assert.AreEqual(BacklogWorkflow.Example.Title, Result(events).Draft!.Title);
    }

    [TestMethod]
    [DataRow("invalid-json")]
    [DataRow("changed-fact")]
    [DataRow("invalid-domain")]
    [DataRow("null")]
    [DataRow("empty")]
    public async Task NativeAgentOutputIsNeverTrustedWithoutValidation(string behavior)
    {
        using var client = new ScriptedClient { Behavior = behavior };
        var events = await Execute(BacklogWorkflow.Build(BacklogWorkflow.CreateReviewer(client)), BacklogWorkflow.Example);
        Assert.AreEqual("Rejected", Result(events).Status);
        Assert.IsNull(Result(events).Draft);
        Assert.AreEqual(1, client.Calls);
    }

    [TestMethod]
    public async Task TransportFailureDoesNotProduceBusinessSuccess()
    {
        using var client = new ScriptedClient { Behavior = "throw" };
        var events = await Execute(BacklogWorkflow.Build(BacklogWorkflow.CreateReviewer(client)), BacklogWorkflow.Example);
        Assert.IsTrue(events.OfType<WorkflowErrorEvent>().Any());
        Assert.IsFalse(events.OfType<WorkflowOutputEvent>().Any(e => e.Data is ReviewResult));
    }

    [TestMethod]
    public async Task FreshGraphsDoNotLeakEarlierInputToAgentSessions()
    {
        using var client = new ScriptedClient();
        var agent = BacklogWorkflow.CreateReviewer(client);
        await Execute(BacklogWorkflow.Build(agent), BacklogWorkflow.Example);
        var next = BacklogWorkflow.Example with { Title = "Export fails", Component = "Reporting" };
        var events = await Execute(BacklogWorkflow.Build(agent), next);
        Assert.AreEqual("Reviewed", Result(events).Status);
        Assert.AreEqual(1, client.LastUserMessages);
    }

    private static async Task<List<WorkflowEvent>> Execute(Workflow workflow, BacklogDraft draft)
    {
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var run = await InProcessExecution.RunStreamingAsync(workflow, draft, cancellationToken: stop.Token);
        var events = new List<WorkflowEvent>();
        await foreach (var evt in run.WatchStreamAsync(stop.Token)) { events.Add(evt); if (evt is WorkflowErrorEvent error) Console.WriteLine(error.Exception); }
        return events;
    }

    private static string[] Path(List<WorkflowEvent> events) => events.OfType<ExecutorInvokedEvent>().Select(e => e.ExecutorId).ToArray();
    private static ReviewResult Result(List<WorkflowEvent> events) => events.OfType<WorkflowOutputEvent>()
        .Select(e => e.Data).OfType<ReviewResult>().Single();

    private sealed class ScriptedClient : IChatClient
    {
        public string Behavior { get; init; } = "echo";
        public int Calls { get; private set; }
        public bool SawSchema { get; private set; }
        public int LastUserMessages { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            SawSchema = options?.ResponseFormat is ChatResponseFormatJson;
            var user = messages.Where(m => m.Role == ChatRole.User).ToArray();
            LastUserMessages = user.Length;
            if (Behavior == "throw") throw new HttpRequestException("Scripted model failure.");
            var text = Behavior switch
            {
                "invalid-json" => "not JSON",
                "empty" => "",
                "null" => "null",
                "changed-fact" => JsonSerializer.Serialize(BacklogWorkflow.Example with { Priority = "low" }),
                "invalid-domain" => JsonSerializer.Serialize(BacklogWorkflow.Example with { Kind = "Delete" }),
                _ => user.Last().Text
            };
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("This graph explicitly disables agent update streaming.");

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}

