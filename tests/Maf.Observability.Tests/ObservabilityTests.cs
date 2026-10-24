using System.Diagnostics;
using System.Text.Json;
using Maf.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.Observability.Tests;

/// <summary>Verifies evidence, privacy, missing counters and correlation without a live model.</summary>
[TestClass]
public sealed class ObservabilityTests
{
    /// <summary>Absence, zero and partial usage remain different values.</summary>
    [TestMethod]
    public void MissingUsageIsNotZero()
    {
        var absent = Consumption.Measure(null, 400);
        Assert.IsNull(absent.Input);
        Assert.IsNull(absent.ReportedTotal);
        Assert.IsNull(absent.DerivedTotal);
        Assert.IsNull(absent.EstimatedCost);
        Assert.AreEqual(100L, absent.CharacterEstimate);
        var zero = Consumption.Measure(new() { InputTokenCount = 0, OutputTokenCount = 0 });
        Assert.AreEqual(0L, zero.DerivedTotal);
        var partial = Consumption.Measure(new() { InputTokenCount = 7 });
        Assert.IsNull(partial.Output);
        Assert.IsNull(partial.DerivedTotal);
    }

    /// <summary>Incomplete request coverage cannot become a complete execution total.</summary>
    [TestMethod]
    public void LedgerSeparatesKnownSubtotalsAndCompleteTotals()
    {
        var ledger = new UsageLedger();
        Assert.IsNull(ledger.Summarize().CompleteInputTotal);
        ledger.Add(Consumption.Measure(new() { InputTokenCount = 0, OutputTokenCount = 2 }));
        Assert.AreEqual(0L, ledger.Summarize().CompleteInputTotal);
        ledger.Add(Consumption.Measure(new() { InputTokenCount = 5 }));
        var summary = ledger.Summarize();
        Assert.AreEqual(5L, summary.CompleteInputTotal);
        Assert.AreEqual(2L, summary.KnownOutputSubtotal);
        Assert.IsNull(summary.CompleteOutputTotal);
        Assert.AreEqual(2, summary.Requests);
        ledger.Add(Consumption.Measure(null));
        Assert.IsNull(ledger.Summarize().CompleteInputTotal);
    }

    /// <summary>A provider total is preserved independently of a derived sum and an explicit price assumption.</summary>
    [TestMethod]
    public void TotalsAndCostsAreAttributed()
    {
        var rate = new TokenRate(1m, 2m, "TEST", "fixture-only-2026-10-24");
        var result = Consumption.Measure(new() { InputTokenCount = 10, OutputTokenCount = 20, TotalTokenCount = 33 }, 100, rate);
        Assert.AreEqual(33L, result.ReportedTotal);
        Assert.AreEqual(30L, result.DerivedTotal);
        Assert.AreEqual(25L, result.CharacterEstimate);
        Assert.AreEqual(0.00005m, result.EstimatedCost);
        Assert.AreEqual(rate.Version, result.RateVersion);
        Assert.IsNull(Consumption.Measure(new() { TotalTokenCount = 30 }, rate: rate).EstimatedCost);
    }

    /// <summary>Invalid measures cannot silently become financial claims.</summary>
    [TestMethod]
    public void InvalidMeasuresAreRejected()
    {
        Assert.Throws<ArgumentException>(() => Consumption.Measure(new() { InputTokenCount = -1 }));
        Assert.Throws<ArgumentException>(() => Consumption.Measure(null, rate: new(-1, 0, "TEST", "v1")));
        Assert.Throws<ArgumentOutOfRangeException>(() => Consumption.Measure(null, -1));
    }

    /// <summary>Arbitrary native names, tags, event data and exception descriptions never reach the safe exporter.</summary>
    [TestMethod]
    public void ExportDropsSecretsEvenInNativeMetadata()
    {
        var spans = new List<SafeSpan>();
        using var provider = Telemetry.CreateProvider(spans.Add);
        using (var activity = Telemetry.Source.StartActivity("SECRET-PROMPT"))
        {
            Assert.IsNotNull(activity);
            activity.SetTag("user.query", "SECRET-PROMPT");
            activity.SetTag("url.full", "https://example.invalid/?key=SECRET-PROMPT");
            activity.AddEvent(new("SECRET-PROMPT"));
            activity.SetStatus(ActivityStatusCode.Error, "SECRET-PROMPT");
        }
        Assert.HasCount(1, spans);
        Assert.IsFalse(JsonSerializer.Serialize(spans).Contains("SECRET-PROMPT", StringComparison.Ordinal));
        Assert.AreEqual("Error", spans[0].Status);
    }

    /// <summary>Native workflow spans and the host tool share a root trace without copying workflow input.</summary>
    [TestMethod]
    public async Task NativeWorkflowAndToolAreCorrelated()
    {
        var spans = new List<SafeSpan>();
        using var provider = Telemetry.CreateProvider(span => { lock (spans) spans.Add(span); });
        string trace;
        using (var root = Telemetry.Source.StartActivity("execution"))
        {
            Assert.IsNotNull(root);
            trace = root.TraceId.ToString();
            await using var run = await InProcessExecution.RunStreamingAsync(BacklogWorkflow.Build(telemetry: true), BacklogWorkflow.Example);
            await foreach (var _ in run.WatchStreamAsync()) { }
            using var tool = Telemetry.Source.StartActivity("tool");
            tool?.SetStatus(ActivityStatusCode.Error);
        }
        Assert.IsTrue(spans.Any(s => s.Source == "maf.workflow"));
        Assert.IsTrue(spans.Any(s => s.Operation == "tool" && s.Status == "Error"));
        Assert.IsTrue(spans.All(s => s.TraceId == trace));
        Assert.IsFalse(JsonSerializer.Serialize(spans).Contains(BacklogWorkflow.Example.Title, StringComparison.Ordinal));
    }

    /// <summary>Replay produces one action and one transactionally committed event, verifiable after reopening.</summary>
    [TestMethod]
    public void CommittedActionSurvivesRestartAndReplay()
    {
        var journal = CreateJournal(out var path);
        var id = Guid.NewGuid();
        journal.Append(id, JournalKind.RequestReceived);
        journal.Append(id, JournalKind.Intention);
        long action = journal.CommitReview(id);
        var reopened = new BusinessJournal(path);
        Assert.AreEqual(action, reopened.CommitReview(id));
        Assert.IsTrue(reopened.HasAction(id, action));
        Assert.AreEqual(1, reopened.Read(id).Count(e => e.Kind == JournalKind.ActionCommitted));
        Assert.AreEqual(action, reopened.Read(id).Last().ActionId);
    }

    /// <summary>Neither an intention nor an acknowledgement may claim a committed effect.</summary>
    [TestMethod]
    public void IntentAndFailureDoNotProveAction()
    {
        var journal = CreateJournal(out _);
        var id = Guid.NewGuid();
        journal.Append(id, JournalKind.Intention);
        journal.Append(id, JournalKind.Acknowledged);
        journal.Append(id, JournalKind.ExecutionFailed);
        Assert.IsFalse(journal.HasAction(id, 1));
        Assert.IsTrue(journal.Read(id).All(e => e.ActionId is null));
        Assert.Throws<ArgumentException>(() => journal.Append(id, JournalKind.ActionCommitted));
        Assert.Throws<ArgumentException>(() => journal.Append(id, (JournalKind)999));
        Assert.Throws<ArgumentException>(() => journal.CommitReview(Guid.Empty));
    }

    /// <summary>Retention removes metadata selectively and never implies undo of business state.</summary>
    [TestMethod]
    public void RetentionAndErasurePreserveBusinessEffect()
    {
        var journal = CreateJournal(out _);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        long action = journal.CommitReview(first);
        journal.Append(second, JournalKind.ExecutionFailed);
        Assert.AreEqual(0, journal.PurgeBefore(DateTimeOffset.UtcNow.AddDays(-1)));
        Assert.AreEqual(1, journal.Erase(first));
        Assert.HasCount(0, journal.Read(first));
        Assert.HasCount(1, journal.Read(second));
        Assert.IsTrue(journal.HasAction(first, action));
        Assert.AreEqual(1, journal.PurgeBefore(DateTimeOffset.UtcNow.AddDays(1)));
    }

    /// <summary>Streaming usage is emitted once, and absent usage remains unknown.</summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task StreamingUsageIsCountedOnce(bool withUsage)
    {
        var reports = new List<Consumption>();
        using var client = new ObservedChatClient(new FixtureClient(withUsage), reports.Add);
        await foreach (var _ in client.GetStreamingResponseAsync([new(ChatRole.User, "SECRET")])) { }
        Assert.HasCount(1, reports);
        Assert.AreEqual(withUsage ? 3L : null, reports[0].Input);
        Assert.AreEqual(withUsage ? 5L : null, reports[0].DerivedTotal);
    }

    /// <summary>Transport exceptions report unknown usage and mark the model span as failed without retaining text.</summary>
    [TestMethod]
    public async Task FailedModelIsUnknownAndSanitized()
    {
        var reports = new List<Consumption>();
        var spans = new List<SafeSpan>();
        using var provider = Telemetry.CreateProvider(spans.Add);
        using var client = new ObservedChatClient(new FixtureClient(false), reports.Add);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetResponseAsync([new(ChatRole.User, "SECRET")]));
        Assert.HasCount(1, reports);
        Assert.IsNull(reports[0].Input);
        Assert.AreEqual("Error", spans.Single().Status);
        Assert.IsFalse(JsonSerializer.Serialize(spans).Contains("SECRET", StringComparison.Ordinal));
    }

    /// <summary>Native MAF tool loops preserve trace ancestry and report each transport request once.</summary>
    [TestMethod]
    public async Task NativeAgentToolLoopHasSafeCorrelatedSpans()
    {
        var reports = new List<Consumption>();
        var spans = new List<SafeSpan>();
        using var provider = Telemetry.CreateProvider(spans.Add);
        using var client = new ObservedChatClient(new ToolLoopClient(), reports.Add);
        int calls = 0;
        var function = AIFunctionFactory.Create(() =>
        {
            using var span = Telemetry.Source.StartActivity("tool");
            calls++;
            return "SECRET-TOOL-RESULT";
        }, name: "read_policy");
        var agent = Telemetry.Instrument(client.AsAIAgent(name: "TestAgent", tools: [function]));
        string trace;
        using (var root = Telemetry.Source.StartActivity("execution"))
        {
            trace = root!.TraceId.ToString();
            await agent.RunAsync("SECRET-PROMPT");
        }
        Assert.AreEqual(1, calls);
        Assert.HasCount(2, reports);
        Assert.IsNull(reports[0].Input);
        Assert.AreEqual(0L, reports[1].Input);
        Assert.IsTrue(spans.Any(s => s.Source == "maf.agent"));
        Assert.IsTrue(spans.Any(s => s.Operation == "tool"));
        Assert.IsTrue(spans.All(s => s.TraceId == trace));
        Assert.IsFalse(JsonSerializer.Serialize(spans).Contains("SECRET", StringComparison.Ordinal));
    }

    /// <summary>An interrupted stream does not present partial counters as a complete request measure.</summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FailedOrCancelledStreamRemainsUnknown(bool cancel)
    {
        var reports = new List<Consumption>();
        using var client = new ObservedChatClient(new InterruptedClient(cancel), reports.Add);
        try
        {
            await foreach (var _ in client.GetStreamingResponseAsync([new(ChatRole.User, "SECRET")])) { }
            Assert.Fail("The fixture must interrupt the stream.");
        }
        catch (Exception error) when (error is OperationCanceledException or InvalidOperationException) { }
        Assert.HasCount(1, reports);
        Assert.IsNull(reports[0].Input);
        Assert.IsNull(reports[0].ReportedTotal);
    }

    /// <summary>The journal keeps trace correlation across reopening without accepting free payloads.</summary>
    [TestMethod]
    public void JournalKeepsTraceAndIsolatesExecutions()
    {
        var journal = CreateJournal(out var path);
        using var provider = Telemetry.CreateProvider(_ => { });
        var id = Guid.NewGuid();
        string trace;
        using (var root = Telemetry.Source.StartActivity("execution"))
        {
            trace = root!.TraceId.ToString();
            journal.Append(id, JournalKind.RequestReceived);
            journal.Append(id, JournalKind.ExecutionCancelled);
        }
        var entries = new BusinessJournal(path).Read(id);
        Assert.IsTrue(entries.All(e => e.TraceId == trace));
        Assert.HasCount(0, journal.Read(Guid.NewGuid()));
        Assert.AreEqual(JournalKind.ExecutionCancelled, entries.Last().Kind);
    }

    private static BusinessJournal CreateJournal(out string path)
    {
        path = Path.Combine(Path.GetTempPath(), "maf-lot013-tests", Guid.NewGuid().ToString("N"), "journal.db");
        return new(path);
    }

    private sealed class FixtureClient(bool withUsage) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("SECRET");

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new(ChatRole.Assistant, "SECRET");
            if (withUsage) yield return new() { Contents = [new UsageContent(new() { InputTokenCount = 3, OutputTokenCount = 2 })] };
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private sealed class ToolLoopClient : IChatClient
    {
        private int calls;
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => Task.FromResult(++calls == 1
                ? new ChatResponse(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call-1", "read_policy")]))
                : new ChatResponse(new ChatMessage(ChatRole.Assistant, "SECRET-ANSWER"))
                    { Usage = new() { InputTokenCount = 0, OutputTokenCount = 0 } });
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private sealed class InterruptedClient(bool cancel) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new() { Contents = [new UsageContent(new() { InputTokenCount = 1 })] };
            if (cancel) throw new OperationCanceledException("SECRET");
            throw new InvalidOperationException("SECRET");
        }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
