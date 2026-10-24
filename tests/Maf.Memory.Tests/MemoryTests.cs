using System.Runtime.CompilerServices;
using System.Text.Json;
using Maf.Memory;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.Memory.Tests;

/// <summary>Tests temporal guarantees against real SQLite, real MAF sessions, and an observable scripted model.</summary>
[TestClass]
public sealed class MemoryTests
{
    private static readonly DateTimeOffset RecordedAt = DateTimeOffset.Parse("2026-10-23T12:00:00Z");

    [TestMethod]
    public void RestartPreservesSqliteContextAndTransactionalResetWorksWithPooling()
    {
        string path = TemporaryFile("state.db");
        var store = new ContextStore(path);
        store.Set("Reporting ' quoted $value");
        var restarted = new ContextStore(path);
        Assert.AreEqual("Reporting ' quoted $value", restarted.Read());
        restarted.Reset();
        Assert.IsNull(store.Read());
        Assert.IsNull(new ContextStore(path).Read());
        restarted.Set("Billing");
        Assert.AreEqual("Billing", store.Read());
    }

    [TestMethod]
    public void CorrectionsReplaceSameKeyAndKeepProvenance()
    {
        var memory = new WorkingMemory();
        Apply(memory, "The module is Reporting.", "module", "Reporting");
        Apply(memory, "Correction: the module is Billing.", "module", "Billing");
        var fact = memory.Snapshot().Single();
        Assert.AreEqual("Billing", fact.Value);
        Assert.AreEqual(2L, fact.Turn);
        Assert.AreEqual(RecordedAt, fact.RecordedAt);
        Assert.AreEqual("Correction: the module is Billing.", fact.Evidence);
        Assert.IsFalse(memory.ToPromptBlock().Contains("Reporting", StringComparison.Ordinal));
    }

    [TestMethod]
    public void StoreEvictsOldestFactsAndBoundsTotalCount()
    {
        var memory = new WorkingMemory();
        foreach (string key in new[] { "product", "module", "priority", "database", "customer", "version" })
            Apply(memory, $"User says {key}.", key, key);
        Assert.AreEqual(5, memory.Snapshot().Count);
        Assert.IsFalse(memory.Snapshot().Any(f => f.Key == "product"));
        Assert.IsTrue(memory.Snapshot().Any(f => f.Key == "version"));
    }

    [TestMethod]
    public void InvalidBatchIsAtomicAndDoesNotPolluteFacts()
    {
        var memory = new WorkingMemory();
        Apply(memory, "Reporting", "module", "Reporting");
        var turn = memory.BeginTurn("Billing");
        Assert.ThrowsExactly<ArgumentException>(() => memory.Apply(turn,
            [new("module", "Billing", "Billing"), new("database", "Imagined", "Billing")], RecordedAt));
        Assert.AreEqual("Reporting", memory.Snapshot().Single().Value);
    }

    [TestMethod]
    [DataRow("unknown", "Billing", "Billing")]
    [DataRow("module", "Billing", "invented evidence")]
    [DataRow("module", "", "Billing")]
    public void RejectsUnknownKeysMissingEvidenceAndEmptyValues(string key, string value, string evidence)
    {
        var memory = new WorkingMemory();
        var turn = memory.BeginTurn("Billing");
        Assert.ThrowsExactly<ArgumentException>(() => memory.Apply(turn, [new(key, value, evidence)], RecordedAt));
        Assert.AreEqual(0, memory.Snapshot().Count);
    }

    [TestMethod]
    public void RejectsOversizedValuesAndBatchesAndDuplicateKeys()
    {
        var memory = new WorkingMemory();
        string longValue = new('x', 257);
        var turn = memory.BeginTurn(longValue);
        Assert.ThrowsExactly<ArgumentException>(() => memory.Apply(turn, [new("module", longValue, longValue)], RecordedAt));
        var candidate = new FactCandidate("module", "x", "x");
        Assert.ThrowsExactly<ArgumentException>(() => memory.Apply(turn, [candidate, candidate], RecordedAt));
        Assert.ThrowsExactly<ArgumentException>(() => memory.Apply(turn, Enumerable.Repeat(candidate, 6).ToArray(), RecordedAt));
    }

    [TestMethod]
    public void ForgottenOrOutOfOrderUpdatesCannotResurrectFacts()
    {
        var memory = new WorkingMemory();
        var old = memory.BeginTurn("Reporting");
        Apply(memory, "Billing", "module", "Billing");
        Assert.ThrowsExactly<InvalidOperationException>(() => memory.Apply(old, [new("module", "Reporting", "Reporting")], RecordedAt));
        var pending = memory.BeginTurn("Reporting");
        memory.Forget();
        Assert.ThrowsExactly<InvalidOperationException>(() => memory.Apply(pending, [new("module", "Reporting", "Reporting")], RecordedAt));
        Assert.AreEqual(0, memory.Snapshot().Count);
    }

    [TestMethod]
    public void TargetedForgetKeepsOtherFactsAndInvalidatesPendingUpdates()
    {
        var memory = new WorkingMemory();
        Apply(memory, "Billing", "module", "Billing");
        Apply(memory, "SQL Server", "database", "SQL Server");
        var turn = memory.BeginTurn("Billing");
        memory.Forget("module");
        Assert.AreEqual("database", memory.Snapshot().Single().Key);
        Assert.ThrowsExactly<InvalidOperationException>(() => memory.Apply(turn, [new("module", "Billing", "Billing")], RecordedAt));
    }

    [TestMethod]
    public async Task SessionRoundTripRestoresMessagesAndDeletePlusFreshSessionForgetsThem()
    {
        using var client = new RecordingClient();
        var agent = client.AsAIAgent(instructions: "Answer briefly.");
        var file = new SessionFile(TemporaryFile("session.json"), "test-1");
        var session = await file.LoadOrCreateAsync(agent);
        await agent.RunAsync("Reporting", session);
        await file.SaveAsync(agent, session);
        var restartedAgent = client.AsAIAgent(instructions: "Answer briefly.");
        var restored = await file.LoadOrCreateAsync(restartedAgent);
        await restartedAgent.RunAsync("Which module?", restored);
        Assert.IsTrue(client.Requests.Last().Any(m => m.Text == "Reporting"));
        file.Delete();
        var fresh = await file.LoadOrCreateAsync(restartedAgent);
        await restartedAgent.RunAsync("Which module?", fresh);
        Assert.IsFalse(client.Requests.Last().Any(m => m.Text.Contains("Reporting", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task CorruptionAndInstructionVersionMismatchAreVisible()
    {
        string path = TemporaryFile("session.json");
        using var client = new RecordingClient();
        var agent = client.AsAIAgent();
        var file = new SessionFile(path, "v1");
        var session = await file.LoadOrCreateAsync(agent);
        await file.SaveAsync(agent, session);
        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => new SessionFile(path, "v2").LoadOrCreateAsync(agent));
        await File.WriteAllTextAsync(path, "invalid json");
        await Assert.ThrowsExactlyAsync<JsonException>(() => file.LoadOrCreateAsync(agent));
    }

    [TestMethod]
    public async Task ExplicitAdmissionRejectsWrongSemanticKeyAndQuestionsAsFacts()
    {
        using var client = new RecordingClient
        {
            ExtractionJson = "{\"facts\":[{\"key\":\"product\",\"value\":\"Reporting\",\"evidence\":\"module=Reporting\"}]}"
        };
        var memory = new WorkingMemory();
        var extraction = new MemoryExtraction(client.AsAIAgent(), memory);
        StringAssert.Contains(await extraction.UpdateAsync("module=Reporting"), "failed");
        client.ExtractionJson = "{\"facts\":[{\"key\":\"module\",\"value\":\"Which module?\",\"evidence\":\"Which module?\"}]}";
        StringAssert.Contains(await extraction.UpdateAsync("Which module?"), "failed");
        Assert.AreEqual(0, memory.Snapshot().Count);
    }

    [TestMethod]
    public async Task ExplicitAdmissionRetainsOnlyFinalCorrectionAndRejectsMissingFacts()
    {
        using var client = new RecordingClient
        {
            ExtractionJson = "{\"facts\":[{\"key\":\"module\",\"value\":\"Billing\",\"evidence\":\"module=Billing\"}]}"
        };
        var memory = new WorkingMemory();
        var extraction = new MemoryExtraction(client.AsAIAgent(), memory);
        StringAssert.Contains(await extraction.UpdateAsync("module=Reporting; module=Billing"), "updated");
        Assert.AreEqual("Billing", memory.Snapshot().Single().Value);
        client.ExtractionJson = "{\"facts\":[]}";
        StringAssert.Contains(await extraction.UpdateAsync("module=Reporting"), "failed");
        Assert.AreEqual("Billing", memory.Snapshot().Single().Value);
    }

    [TestMethod]
    public async Task ExtractionFailureRetainsPreviousFactsAndReportsNoFallback()
    {
        var memory = new WorkingMemory();
        Apply(memory, "Reporting", "module", "Reporting");
        using var client = new RecordingClient { ExtractionJson = "broken" };
        var extraction = new MemoryExtraction(client.AsAIAgent(), memory);
        string status = await extraction.UpdateAsync("Billing");
        StringAssert.Contains(status, "failed");
        StringAssert.Contains(status, "No fallback");
        Assert.AreEqual("Reporting", memory.Snapshot().Single().Value);
    }

    [TestMethod]
    public async Task WriterUsesOnlyCurrentUserAndNoConversationSession()
    {
        using var client = new RecordingClient();
        var extraction = new MemoryExtraction(client.AsAIAgent(instructions: MemoryInstructions.Extractor), new WorkingMemory());
        await extraction.UpdateAsync("Reporting");
        await extraction.UpdateAsync("Billing");
        Assert.IsFalse(client.Requests.Last().Any(m => m.Text.Contains("Reporting", StringComparison.Ordinal)));
        Assert.IsFalse(client.Requests.Last().Any(m => m.Role == ChatRole.Assistant));
    }

    [TestMethod]
    public async Task ExtractionCancellationIsNotHidden()
    {
        using var client = new RecordingClient();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var extraction = new MemoryExtraction(client.AsAIAgent(), new WorkingMemory());
        await Assert.ThrowsAsync<OperationCanceledException>(() => extraction.UpdateAsync("Reporting", cancellation.Token));
    }

    [TestMethod]
    public async Task ProviderDoesNotExtractAssistantAnswersAndForgetClearsBothHistories()
    {
        using var business = new RecordingClient { Answer = "Assistant inferred exports." };
        using var writer = new RecordingClient
        {
            ExtractionJson = "{\"facts\":[{\"key\":\"module\",\"value\":\"Reporting\",\"evidence\":\"Reporting\"}]}"
        };
        var memory = new WorkingMemory();
        var extraction = new MemoryExtraction(writer.AsAIAgent(instructions: MemoryInstructions.Extractor), memory);
        var diagnostics = new List<string>();
        AIAgent Create() => business.AsAIAgent(new ChatClientAgentOptions
        {
            AIContextProviders = [new SharedMemoryProvider(memory, extraction, diagnostics.Add)]
        });
        var backlog = Create();
        var knowledge = Create();
        var first = await backlog.CreateSessionAsync();
        var second = await knowledge.CreateSessionAsync();
        await backlog.RunAsync("module=Reporting", first);
        await knowledge.RunAsync("module=Reporting", second);
        Assert.AreEqual(1, memory.Snapshot().Count);
        Assert.AreEqual(2, diagnostics.Count);
        Assert.IsFalse(writer.Requests.SelectMany(r => r).Any(m => m.Text.Contains("inferred exports", StringComparison.Ordinal)));
        memory.Forget();
        first = await backlog.CreateSessionAsync();
        second = await knowledge.CreateSessionAsync();
        writer.ExtractionJson = "{\"facts\":[]}";
        await backlog.RunAsync("Which module?", first);
        await knowledge.RunAsync("Which module?", second);
        Assert.IsFalse(business.Requests.TakeLast(2).SelectMany(r => r).Any(m => m.Text.Contains("Reporting", StringComparison.Ordinal)));
        Assert.AreEqual(0, memory.Snapshot().Count);
    }

    [TestMethod]
    public async Task RequestCompactionDropsOldMessagesButPreservesSerializedSourceHistory()
    {
        using var client = new RecordingClient();
        using var compacted = NativeCompaction.CreateRequestView(client);
        var agent = compacted.AsAIAgent();
        var session = await agent.CreateSessionAsync();
        for (int i = 0; i < 8; i++)
            await agent.RunAsync($"unique-turn-{i}", session);
        Assert.IsFalse(client.Requests.Last().Any(m => m.Text == "unique-turn-0"));
        Assert.IsTrue(client.Requests.Last().Any(m => m.Text == "unique-turn-7"));
        var serialized = await agent.SerializeSessionAsync(session);
        StringAssert.Contains(serialized.GetRawText(), "unique-turn-0");
    }

    [TestMethod]
    public async Task HistoryReducerAlsoDropsOldMessagesFromSavedSessions()
    {
        using var client = new RecordingClient();
        var agent = client.AsAIAgent(new ChatClientAgentOptions { ChatHistoryProvider = NativeCompaction.CreateBoundedHistory() });
        var session = await agent.CreateSessionAsync();
        for (int i = 0; i < 8; i++)
            await agent.RunAsync($"unique-turn-{i}", session);
        var serialized = await agent.SerializeSessionAsync(session);
        Assert.IsFalse(serialized.GetRawText().Contains("unique-turn-0", StringComparison.Ordinal));
        StringAssert.Contains(serialized.GetRawText(), "unique-turn-7");
        var restored = await agent.DeserializeSessionAsync(serialized);
        await agent.RunAsync("next", restored);
        Assert.IsFalse(client.Requests.Last().Any(m => m.Text == "unique-turn-0"));
    }

    private static void Apply(WorkingMemory memory, string text, string key, string value)
        => memory.Apply(memory.BeginTurn(text), [new(key, value, text)], RecordedAt);

    private static string TemporaryFile(string fileName)
    {
        string directory = Path.Combine(Path.GetTempPath(), "MafMemoryTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, fileName);
    }

    private sealed class RecordingClient : IChatClient
    {
        public List<List<ChatMessage>> Requests { get; } = [];
        public string ExtractionJson { get; set; } = "{\"facts\":[]}";
        public string Answer { get; init; } = "Acknowledged.";

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(messages.ToList());
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                options?.ResponseFormat is ChatResponseFormatJson ? ExtractionJson : Answer)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new(ChatRole.Assistant, Answer);
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }
}
