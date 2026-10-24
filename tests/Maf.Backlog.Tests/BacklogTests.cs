using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.Backlog.Tests;

/// <summary>Exercises host business rules, actual SQLite effects and the native MAF approval loop.</summary>
[TestClass]
public sealed class BacklogTests
{
    private string path = null!;
    private BacklogStore store = null!;
    private static BacklogDraft Valid => new("Login fails", "Bug", "high", "Auth", "Valid credentials are rejected.", []);

    [TestInitialize]
    public void Initialize()
    {
        path = Path.Combine(Path.GetTempPath(), $"maf-lot005-{Guid.NewGuid():N}.db");
        store = new(path);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(path);
    }

    [TestMethod]
    [DataRow("title")]
    [DataRow("summary")]
    [DataRow("kind")]
    [DataRow("priority")]
    [DataRow("component")]
    [DataRow("nullQuestions")]
    [DataRow("ambiguous")]
    [DataRow("tooManyQuestions")]
    [DataRow("emptyQuestion")]
    [DataRow("longTitle")]
    [DataRow("longSummary")]
    public void DeserializableInvalidBusinessContractCannotWrite(string field)
    {
        var draft = field switch
        {
            "title" => Valid with { Title = null! },
            "summary" => Valid with { Summary = " " },
            "kind" => Valid with { Kind = "Refund" },
            "priority" => Valid with { Priority = "urgent" },
            "component" => Valid with { Component = "Payments" },
            "nullQuestions" => Valid with { ClarifyingQuestions = null! },
            "ambiguous" => Valid with { ClarifyingQuestions = ["Which account?"] },
            "tooManyQuestions" => Valid with { ClarifyingQuestions = ["A?", "B?", "C?"] },
            "emptyQuestion" => Valid with { ClarifyingQuestions = [null!] },
            "longTitle" => Valid with { Title = new string('x', 121) },
            _ => Valid with { Summary = new string('x', 2001) }
        };
        var typed = JsonSerializer.Deserialize<BacklogDraft>(JsonSerializer.Serialize(draft))!;
        var gate = new WriteGate(store, typed, true);
        Assert.IsFalse(gate.Approve("create_backlog_item", gate.ProposalId, true));
        Assert.AreEqual("Invalid", gate.Create(gate.ProposalId).Status);
        Assert.ThrowsExactly<ArgumentException>(() => store.Insert(typed));
        Assert.HasCount(0, store.List());
    }

    [TestMethod]
    public void FixedDraftIsStoredAndApprovalCannotBeReused()
    {
        var draft = Valid;
        var gate = new WriteGate(store, draft, true);
        Assert.IsTrue(gate.Approve("create_backlog_item", gate.ProposalId, true));
        var result = gate.Create(gate.ProposalId);
        Assert.AreEqual("Created", result.Status);
        var row = store.List().Single();
        Assert.AreEqual(result.ItemId, row.Id);
        Assert.AreEqual(draft.Title, row.Title);
        Assert.AreEqual(draft.Summary, row.Summary);
        Assert.AreEqual(draft.Kind, row.Kind);
        Assert.AreEqual(draft.Priority, row.Priority);
        Assert.AreEqual(draft.Component, row.Component);
        Assert.AreEqual("Refused", gate.Create(gate.ProposalId).Status);
        Assert.IsFalse(gate.Approve("create_backlog_item", gate.ProposalId, true));
        Assert.HasCount(1, store.List());
    }

    [TestMethod]
    public void HostPermissionAndExactProposalRemainRequired()
    {
        var gate = new WriteGate(store, Valid, false);
        Assert.IsFalse(gate.Approve("create_backlog_item", gate.ProposalId, true));
        Assert.AreEqual("Refused", gate.Create(gate.ProposalId).Status);
        var allowed = new WriteGate(store, Valid, true);
        Assert.IsFalse(allowed.Approve("delete_database", allowed.ProposalId, true));
        Assert.IsFalse(allowed.Approve("create_backlog_item", gate.ProposalId, true));
        Assert.IsFalse(allowed.Approve("create_backlog_item", allowed.ProposalId, false));
        Assert.AreEqual("Refused", allowed.Create(allowed.ProposalId).Status);
        Assert.IsTrue(allowed.Approve("create_backlog_item", allowed.ProposalId, true));
        Assert.AreEqual("Refused", allowed.Create(gate.ProposalId).Status);
        Assert.HasCount(0, store.List());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task NativeMafApprovalControlsActualSqliteWrite(bool accept)
    {
        var gate = new WriteGate(store, Valid, true);
        using var client = new ToolClient(gate.ProposalId);
        var agent = Writer(client, gate);
        var session = await agent.CreateSessionAsync();
        var response = await agent.RunAsync("Create the reviewed proposal.", session);
        Assert.HasCount(0, gate.Observed);
        Assert.HasCount(0, store.List());
        var request = Requests(response).Single();
        var review = new ApprovalReview(session, gate);
        review.Register(response);
        var decision = review.Decide(session, request, accept);
        Assert.ThrowsExactly<InvalidOperationException>(() => review.Decide(session, request, true));
        await agent.RunAsync(new ChatMessage(ChatRole.User, [decision]), session);
        Assert.AreEqual(accept ? 1 : 0, store.List().Count);
        Assert.AreEqual(accept ? 1 : 0, gate.Observed.Count);
        Assert.IsTrue(client.SawResult);
    }

    [TestMethod]
    public async Task MissingResponseAndForeignSessionDoNotAuthorize()
    {
        var gate = new WriteGate(store, Valid, true);
        using var client = new ToolClient(gate.ProposalId);
        var agent = Writer(client, gate);
        var session = await agent.CreateSessionAsync();
        var response = await agent.RunAsync("Create", session);
        var review = new ApprovalReview(session, gate);
        review.Register(response);
        var request = Requests(response).Single();
        var otherSession = await agent.CreateSessionAsync();
        Assert.ThrowsExactly<InvalidOperationException>(() => review.Decide(otherSession, request, true));
        Assert.AreEqual("Refused", gate.Create(gate.ProposalId).Status);
        Assert.HasCount(0, store.List());
    }

    [TestMethod]
    public async Task MultipleRequestsAreReviewedSeparatelyAndCannotDoubleWrite()
    {
        var gate = new WriteGate(store, Valid, true);
        using var client = new ToolClient(gate.ProposalId) { Multiple = true };
        var agent = Writer(client, gate);
        var session = await agent.CreateSessionAsync();
        var response = await agent.RunAsync("Create", session);
        var review = new ApprovalReview(session, gate);
        review.Register(response);
        var requests = Requests(response);
        Assert.HasCount(2, requests);
        var decisions = requests.Select(r => review.Decide(session, r, true)).ToList();
        await agent.RunAsync(new ChatMessage(ChatRole.User, decisions), session);
        Assert.HasCount(1, store.List());
        Assert.AreEqual(1, gate.Observed.Count(r => r.Status == "Created"));
    }

    [TestMethod]
    public async Task ChangedArgumentsAndRepeatedRegistrationAreRejected()
    {
        var gate = new WriteGate(store, Valid, true);
        using var client = new ToolClient(gate.ProposalId);
        var agent = Writer(client, gate);
        var session = await agent.CreateSessionAsync();
        var response = await agent.RunAsync("Create", session);
        var review = new ApprovalReview(session, gate);
        review.Register(response);
        Assert.ThrowsExactly<InvalidOperationException>(() => review.Register(response));
        var request = Requests(response).Single();
        ((FunctionCallContent)request.ToolCall).Arguments!["proposalId"] = "another-proposal";
        Assert.ThrowsExactly<InvalidOperationException>(() => review.Decide(session, request, true));
        Assert.HasCount(0, store.List());
    }

    [TestMethod]
    public async Task GeneratedCreationClaimWithoutToolIsNotEvidence()
    {
        var gate = new WriteGate(store, Valid, true);
        using var client = new ToolClient(gate.ProposalId) { NoTool = true };
        var response = await Writer(client, gate).RunAsync("Create");
        Assert.IsTrue(response.Text.Contains("Created"));
        Assert.HasCount(0, Requests(response));
        Assert.HasCount(0, gate.Observed);
        Assert.HasCount(0, store.List());
    }

    [TestMethod]
    public async Task UnknownToolCannotReachTheWriteBoundary()
    {
        var gate = new WriteGate(store, Valid, true);
        using var client = new ToolClient(gate.ProposalId) { UnknownTool = true };
        var response = await Writer(client, gate).RunAsync("Create");
        Assert.HasCount(0, Requests(response));
        Assert.HasCount(0, gate.Observed);
        Assert.HasCount(0, store.List());
        Assert.IsTrue(client.SawResult);
    }

    [TestMethod]
    public void MissingRefusedAndErroredLookupNeverInventAValue()
    {
        Assert.AreEqual("42 EUR", LookupResult.SimulatedInvoice("INV-001").ToAnswer());
        Assert.AreEqual("NotFound", LookupResult.SimulatedInvoice("INV-404").Status);
        Assert.IsFalse(LookupResult.SimulatedInvoice("INV-404").ToAnswer().Contains("42"));
        Assert.IsTrue(new LookupResult("Refused", null).ToAnswer().Contains("refused"));
        Assert.IsTrue(new LookupResult("Error", null).ToAnswer().Contains("failed"));
    }

    [TestMethod]
    public void StorageFailureIsAnObservedErrorWithNoCreatedId()
    {
        var unavailable = new BacklogStore(Path.Combine(path, "child.db"));
        File.WriteAllText(path, "This file prevents the parent directory from being created.");
        var gate = new WriteGate(unavailable, Valid, true);
        Assert.IsTrue(gate.Approve("create_backlog_item", gate.ProposalId, true));
        var result = gate.Create(gate.ProposalId);
        Assert.AreEqual("Error", result.Status);
        Assert.IsNull(result.ItemId);
        Assert.AreEqual("Refused", gate.Create(gate.ProposalId).Status);
    }

    [TestMethod]
    public void ChangingTheOriginalQuestionArrayCannotChangeTheReviewedDraft()
    {
        var questions = new[] { "Which account?" };
        var gate = new WriteGate(store, Valid with { ClarifyingQuestions = questions }, true);
        questions[0] = "";
        Assert.IsFalse(gate.Approve("create_backlog_item", gate.ProposalId, true));
        Assert.AreEqual("Invalid", gate.Create(gate.ProposalId).Status);
        Assert.HasCount(0, store.List());
    }

    private static AIAgent Writer(IChatClient client, WriteGate gate) => client.AsAIAgent(name: "TestWriter",
        tools: [new ApprovalRequiredAIFunction(AIFunctionFactory.Create(gate.Create, "create_backlog_item"))]);

    private static ToolApprovalRequestContent[] Requests(AgentResponse response) => response.Messages
        .SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>().ToArray();

    private sealed class ToolClient(string proposalId) : IChatClient
    {
        public bool Multiple { get; init; }
        public bool NoTool { get; init; }
        public bool UnknownTool { get; init; }
        public bool SawResult { get; private set; }
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SawResult = messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Any();
            if (SawResult || NoTool) return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Created (model declaration only).")));
            var calls = new List<AIContent> { new FunctionCallContent("call-1", UnknownTool ? "delete_database" : "create_backlog_item", new Dictionary<string, object?> { ["proposalId"] = proposalId }) };
            if (Multiple) calls.Add(new FunctionCallContent("call-2", "create_backlog_item", new Dictionary<string, object?> { ["proposalId"] = proposalId }));
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, calls)));
        }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
