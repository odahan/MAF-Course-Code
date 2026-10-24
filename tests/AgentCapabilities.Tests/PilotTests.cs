using System.Runtime.CompilerServices;
using Maf.AgentCapabilities;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.AgentCapabilities.Tests;

/// <summary>Reuses EP004's deterministic contract cases and exercises the actual MAF pilot loop.</summary>
[TestClass]
public sealed class PilotTests
{
    [TestMethod]
    [DataRow("create_ticket", 10, "Simple support request")]
    [DataRow("human_review", 85, "Sensitive request")]
    public void AcceptsValidDecisions(string action, int risk, string reason)
        => new TicketDecision(action, risk, reason).Validate();

    [TestMethod]
    [DataRow("refund", 20, "Forbidden action")]
    [DataRow("create_ticket", 150, "Invalid risk")]
    [DataRow("create_ticket", -1, "Invalid risk")]
    [DataRow("create_ticket", 5, "")]
    [DataRow("create_ticket", 5, null)]
    public void RejectsInvalidDecisions(string action, int risk, string? reason)
        => Assert.ThrowsExactly<InvalidOperationException>(() => new TicketDecision(action, risk, reason!).Validate());

    [TestMethod]
    public void UsageDistinguishesMissingPartialAndZero()
    {
        Assert.IsNull(UsageSnapshot.From(null));
        var partial = UsageSnapshot.From(new() { InputTokenCount = 0 });
        Assert.AreEqual(0L, partial!.Input);
        Assert.IsNull(partial.Output);
        Assert.IsNull(partial.Total);
    }

    [TestMethod]
    public async Task AgentSendsSchemaExecutesToolAndCancelsStream()
    {
        using var fake = new PilotClient();
        var results = await new CapabilityPilot(fake).RunAsync();
        Assert.AreEqual(5, results.Count);
        Assert.IsTrue(results.All(r => r.Status == "Passed"), string.Join(",", results));
        Assert.IsTrue(fake.SawSchema);
        Assert.IsTrue(fake.SawToolResult);
        Assert.IsTrue(fake.SawCancellation);
        var usage = results.Single(r => r.Capability == "tools").Usage!;
        Assert.AreEqual(4L, usage.Input);
        Assert.AreEqual(6L, usage.Output);
        Assert.AreEqual(10L, usage.Total);
    }

    [TestMethod]
    public async Task MissingToolAndInvalidStructureRemainFailures()
    {
        using var fake = new PilotClient { BreakContract = true };
        var results = await new CapabilityPilot(fake).RunAsync();
        Assert.AreEqual("Failed", results.Single(r => r.Capability == "structure").Status);
        Assert.AreEqual("Failed", results.Single(r => r.Capability == "tools").Status);
        Assert.AreEqual("Passed", results.Single(r => r.Capability == "streaming").Status);
    }

    [TestMethod]
    public async Task IgnoredCancellationIsFailure()
    {
        using var fake = new PilotClient { IgnoreCancellation = true };
        var results = await new CapabilityPilot(fake).RunAsync();
        Assert.AreEqual("Failed", results.Single(r => r.Capability == "cancellation").Status);
    }

    [TestMethod]
    public async Task CallerCancellationPropagates()
    {
        using var fake = new PilotClient();
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => new CapabilityPilot(fake).RunAsync(stop.Token));
    }

    private sealed class PilotClient : IChatClient
    {
        public bool BreakContract { get; init; }
        public bool IgnoreCancellation { get; init; }
        public bool SawSchema { get; private set; }
        public bool SawToolResult { get; private set; }
        public bool SawCancellation { get; private set; }
        private int responseCalls;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (++responseCalls > 10) throw new InvalidOperationException("Unexpected repeated model call.");
            ChatMessage message = new(ChatRole.Assistant, "Hello.");
            if (options?.ResponseFormat is ChatResponseFormatJson schema)
            {
                SawSchema = schema.Schema is not null;
                message = new(ChatRole.Assistant, BreakContract
                    ? "{\"action\":\"refund\",\"riskScore\":200,\"reason\":\"\"}"
                    : "{\"action\":\"create_ticket\",\"riskScore\":10,\"reason\":\"Simple request\"}");
            }
            else if (options?.Tools?.Count > 0 && !BreakContract)
            {
                var result = messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>().LastOrDefault();
                if (result is null)
                    message = new(ChatRole.Assistant, [new FunctionCallContent("receipt-1", "ReadReceipt", new Dictionary<string, object?>())]);
                else
                {
                    SawToolResult = true;
                    message = new(ChatRole.Assistant, result.Result?.ToString());
                }
            }
            return Task.FromResult(new ChatResponse(message)
            {
                Usage = new() { InputTokenCount = 2, OutputTokenCount = 3, TotalTokenCount = 5 }
            });
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new(ChatRole.Assistant, "First text.");
            if (cancellationToken.IsCancellationRequested)
            {
                SawCancellation = true;
                if (!IgnoreCancellation) cancellationToken.ThrowIfCancellationRequested();
            }
            yield return new(ChatRole.Assistant, "Second text.");
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }
}
