using System.Text.Json;
using LocalMcp;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;

namespace LocalMcp.Tests;

/// <summary>Exercises real stdio MCP and native MAF orchestration without a model service.</summary>
[TestClass]
public sealed class McpTests
{
    private static string ServerPath
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BaseTechnique.slnx")))
                directory = directory.Parent;
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            return Path.Combine(directory!.FullName, "demos/Demo400_LocalMcp/Server/bin", configuration, "net10.0/ServeurMcpLocal.dll");
        }
    }

    private static Task<LocalMcpSession> Connect() => LocalMcpSession.ConnectAsync(ServerPath, TimeSpan.FromSeconds(10));
    private static JsonElement Payload(CallToolResult result)
    {
        Assert.AreNotEqual(true, result.IsError);
        return JsonDocument.Parse(string.Join("", result.Content.OfType<TextContentBlock>().Select(c => c.Text))).RootElement.Clone();
    }

    [TestMethod]
    public async Task DiscoveryPublishesOnlyReadToolsAndRequiredInput()
    {
        await using var session = await Connect();
        CollectionAssert.AreEquivalent(new[] { "list_priority_customers", "get_incident_status" }, session.Tools.Select(t => t.Name).ToArray());
        var schema = session.Tools.Single(t => t.Name == "get_incident_status").JsonSchema;
        Assert.AreEqual("string", schema.GetProperty("properties").GetProperty("incidentId").GetProperty("type").GetString());
        Assert.AreEqual("incidentId", schema.GetProperty("required")[0].GetString());
    }

    [TestMethod]
    public async Task CustomerCatalogueCrossesProcessBoundary()
    {
        await using var session = await Connect();
        var data = Payload(await session.CallAsync("list_priority_customers", new()));
        Assert.AreEqual(3, data.GetArrayLength());
        Assert.AreEqual("Atlas Retail", data[0].GetProperty("name").GetString());
    }

    [TestMethod]
    [DataRow("INC-318", "Found", "Critical")]
    [DataRow("inc-204", "Found", "High")]
    [DataRow("INC-999", "NotFound", null)]
    public async Task LookupPreservesFoundAndMissing(string id, string status, string? impact)
    {
        await using var session = await Connect();
        var data = Payload(await session.CallAsync("get_incident_status", new() { ["incidentId"] = id }));
        Assert.AreEqual(status, data.GetProperty("status").GetString());
        if (impact is not null) Assert.AreEqual(impact, data.GetProperty("incident").GetProperty("impact").GetString());
        else Assert.IsFalse(data.TryGetProperty("incident", out var incident) && incident.ValueKind != JsonValueKind.Null);
    }

    [TestMethod]
    [DataRow("garbage")]
    [DataRow("INC-318\n")]
    [DataRow("")]
    public async Task InvalidIdIsMcpToolErrorAndConnectionRemainsUsable(string id)
    {
        await using var session = await Connect();
        var result = await session.CallAsync("get_incident_status", new() { ["incidentId"] = id });
        Assert.IsTrue(result.IsError == true);
        Assert.AreEqual("Found", Payload(await session.CallAsync("get_incident_status", new() { ["incidentId"] = "INC-318" })).GetProperty("status").GetString());
    }

    [TestMethod]
    public async Task MissingRequiredArgumentIsToolError()
    {
        await using var session = await Connect();
        Assert.IsTrue((await session.CallAsync("get_incident_status", new())).IsError == true);
    }

    [TestMethod]
    public async Task HostRejectsToolOutsideAllowlist()
    {
        await using var session = await Connect();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => session.CallAsync("delete_incident", new()));
        Assert.AreEqual(3, Payload(await session.CallAsync("list_priority_customers", new())).GetArrayLength());
    }

    [TestMethod]
    public async Task MissingServerFailsBeforeLaunch()
        => await Assert.ThrowsAsync<FileNotFoundException>(() => LocalMcpSession.ConnectAsync(ServerPath + ".missing", TimeSpan.FromSeconds(1)));

    [TestMethod]
    public async Task InitializationHonorsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => LocalMcpSession.ConnectAsync(ServerPath, TimeSpan.FromSeconds(10), cancellation.Token));
    }

    [TestMethod]
    public async Task ToolCallHonorsCallerCancellation()
    {
        await using var session = await Connect();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => session.CallAsync("list_priority_customers", new(), cancellation.Token));
        Assert.AreEqual(3, Payload(await session.CallAsync("list_priority_customers", new())).GetArrayLength());
    }

    [TestMethod]
    public async Task NativeAgentInvokesDiscoveredToolsAndReceivesActualResults()
    {
        await using var session = await Connect();
        using var model = new ScriptedSupportClient();
        var outcome = await session.RunAgentAsync(model, "Perform all three lookups.");
        Assert.AreEqual("Completed", outcome.Status);
        Assert.AreEqual(3, outcome.Calls.Length);
        Assert.IsTrue(model.SawCritical && model.SawNotFound && model.SawCustomers);
        Assert.AreEqual(4, model.Calls);
    }

    [TestMethod]
    public async Task AssistantClaimWithoutToolUseIsIncomplete()
    {
        await using var session = await Connect();
        using var model = new ScriptedSupportClient("claim");
        var outcome = await session.RunAgentAsync(model, "Perform all three lookups.");
        Assert.AreEqual("Incomplete", outcome.Status);
        Assert.AreEqual(0, outcome.Calls.Length);
    }

    [TestMethod]
    public async Task SingleLookupCannotClaimWholeTaskComplete()
    {
        await using var session = await Connect();
        using var model = new ScriptedSupportClient("single");
        Assert.AreEqual("Incomplete", (await session.RunAgentAsync(model, "Perform all three lookups.")).Status);
    }

    [TestMethod]
    public async Task NativeModelCallPreservesCancellation()
    {
        await using var session = await Connect();
        using var model = new ScriptedSupportClient("wait");
        using var cancellation = new CancellationTokenSource();
        var run = session.RunAgentAsync(model, "Wait.", cancellation.Token);
        await model.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => run);
    }

    [TestMethod]
    public async Task ToolLoopStopsAtHostBudget()
    {
        await using var session = await Connect();
        using var model = new ScriptedSupportClient("repeat");
        var result = await session.RunAgentAsync(model, "Keep looking up.");
        Assert.AreEqual("ToolBudget", result.Status);
        Assert.AreEqual(6, result.Calls.Length);
        Assert.IsTrue(model.Calls <= 7);
    }

    [TestMethod]
    public async Task RunDeadlineIsDistinctFromCallerCancellation()
    {
        await using var session = await Connect();
        using var model = new ScriptedSupportClient("wait");
        var result = await session.RunAgentAsync(model, "Wait.", runTimeout: TimeSpan.FromMilliseconds(50));
        Assert.AreEqual("DeadlineExceeded", result.Status);
    }

    [TestMethod]
    public async Task NativeToolErrorIsRecordedAsFailure()
    {
        await using var session = await Connect();
        using var model = new ScriptedSupportClient("invalid");
        var result = await session.RunAgentAsync(model, "Perform all three lookups.");
        Assert.AreEqual("Incomplete", result.Status);
        Assert.IsTrue(result.Calls.Any(c => !c.Succeeded));
    }

    [TestMethod]
    public async Task DisposalClosesServerProcess()
    {
        var session = await Connect();
        await session.DisposeAsync();
        var completion = await session.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.IsInstanceOfType<ModelContextProtocol.Client.StdioClientCompletionDetails>(completion);
        var details = (ModelContextProtocol.Client.StdioClientCompletionDetails)completion;
        Assert.IsNotNull(details.ExitCode);
    }

    [TestMethod]
    public async Task InitializationDeadlineStopsHandshake()
        => await Assert.ThrowsAsync<OperationCanceledException>(() =>
            LocalMcpSession.ConnectAsync(ServerPath, TimeSpan.FromMilliseconds(1)));
}

/// <summary>Scripts provider requests while MAF and MCP execute their actual implementations.</summary>
internal sealed class ScriptedSupportClient(string scenario = "complete") : IChatClient
{
    public int Calls { get; private set; }
    public bool SawCritical { get; private set; }
    public bool SawNotFound { get; private set; }
    public bool SawCustomers { get; private set; }
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        Entered.TrySetResult();
        if (scenario == "wait") await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        var results = string.Join(" ", messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().Select(r => JsonSerializer.Serialize(r.Result)));
        SawCritical |= results.Contains("Critical");
        SawNotFound |= results.Contains("NotFound");
        SawCustomers |= results.Contains("Atlas Retail");
        if (scenario == "claim" || (scenario == "single" && Calls > 1) || (scenario is "complete" or "invalid" && Calls > 3))
            return new(new ChatMessage(ChatRole.Assistant, "Lookups finished."));
        var name = Calls == 1 && scenario != "repeat" ? "list_priority_customers" : "get_incident_status";
        var arguments = name == "list_priority_customers" ? new Dictionary<string, object?>()
            : new Dictionary<string, object?> { ["incidentId"] = scenario == "invalid" ? "bad" : Calls == 3 ? "INC-999" : "INC-318" };
        return new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("call-" + Calls, name, arguments)]));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("scripted-support") : null;
    public void Dispose() { }
}
