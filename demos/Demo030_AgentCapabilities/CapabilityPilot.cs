using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Maf.AgentCapabilities;

/// <summary>Retains missing usage counters as null, including when other counters are present.</summary>
public sealed record UsageSnapshot(long? Input, long? Output, long? Total)
{
    /// <summary>Copies provider counters without estimating tokens or money.</summary>
    public static UsageSnapshot? From(UsageDetails? usage) => usage is null ? null
        : new(usage.InputTokenCount, usage.OutputTokenCount, usage.TotalTokenCount);
}

/// <summary>Records one observed capability without turning failures into unsupported successes.</summary>
public sealed record CapabilityResult(string Capability, string Status, string Evidence, UsageSnapshot? Usage);

/// <summary>Defines a small typed contract whose business rules remain host responsibilities.</summary>
public sealed record TicketDecision(string Action, int RiskScore, string Reason)
{
    /// <summary>Rejects invalid domains, bounds and required text before any action.</summary>
    public void Validate()
    {
        if (Action is not ("create_ticket" or "human_review") || RiskScore is < 0 or > 100
            || string.IsNullOrWhiteSpace(Reason) || Reason.Length > 300)
            throw new InvalidOperationException("Invalid ticket decision.");
    }
}

/// <summary>Exercises MAF agents through the shared connection with no fallback or external side effect.</summary>
public sealed class CapabilityPilot(IChatClient client, bool disableReasoning = false)
{
    /// <summary>Runs independent probes; all failures stay visible and each request has a finite deadline.</summary>
    public async Task<IReadOnlyList<CapabilityResult>> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<CapabilityResult>();
        await ProbeAsync("text", async token =>
        {
            var response = await CreateAgent("TextPilot", "Answer in English.")
                .RunAsync("Say hello in one short sentence.", cancellationToken: token);
            Require(!string.IsNullOrWhiteSpace(response.Text), "Empty agent response.");
            return new("text", "Passed", response.Text, UsageSnapshot.From(response.Usage));
        });
        await ProbeAsync("structure", async token =>
        {
            var agent = CreateAgent("StructuredPilot", schema: ChatResponseFormat.ForJsonSchema<TicketDecision>());
            var response = await agent.RunAsync(
                "Classify a simple password help request. Action must be create_ticket or human_review, " +
                "RiskScore from 0 to 100, Reason a short nonempty English explanation.", cancellationToken: token);
            var decision = JsonSerializer.Deserialize<TicketDecision>(response.Text, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidOperationException("Missing decision.");
            decision.Validate();
            return new("structure", "Passed", response.Text, UsageSnapshot.From(response.Usage));
        });
        await ProbeAsync("tools", async token =>
        {
            var receipt = Guid.NewGuid().ToString("N");
            var calls = 0;
            [Description("Reads the current verification receipt from the host. No arguments required.")]
            string ReadReceipt() { calls++; return receipt; }
            var agent = CreateAgent(name: "ToolPilot",
                instructions: "Call ReadReceipt and return its exact result. Never invent a receipt.",
                tools: [AIFunctionFactory.Create(ReadReceipt, "ReadReceipt")]);
            var response = await agent.RunAsync("Read the current verification receipt.", cancellationToken: token);
            Require(calls > 0 && response.Text.Contains(receipt, StringComparison.Ordinal),
                "The host tool must execute and its unpredictable result must reach the final response.");
            return new("tools", "Passed", $"Host calls: {calls}; receipt: {receipt}; response: {response.Text}",
                UsageSnapshot.From(response.Usage));
        });
        await ProbeAsync("streaming", async token =>
        {
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in CreateAgent("StreamPilot").RunStreamingAsync(
                "Write three short sentences about software testing.", cancellationToken: token))
                updates.Add(update);
            var response = updates.ToAgentResponse();
            Require(!string.IsNullOrWhiteSpace(response.Text), "Empty stream.");
            return new("streaming", "Passed", $"Updates: {updates.Count}; text: {response.Text}",
                UsageSnapshot.From(response.Usage));
        });
        await ProbeAsync("cancellation", async token =>
        {
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
            var agent = CreateAgent("CancellationPilot");
            var sawText = false;
            var timer = Stopwatch.StartNew();
            try
            {
                await foreach (var update in agent.RunStreamingAsync(
                    "Write a detailed numbered list of 200 software testing tips.", cancellationToken: stop.Token))
                {
                    Require(!stop.IsCancellationRequested || string.IsNullOrEmpty(update.Text),
                        "The provider continued producing text after cancellation.");
                    if (!string.IsNullOrEmpty(update.Text) && !stop.IsCancellationRequested)
                    {
                        sawText = true;
                        timer.Restart();
                        stop.Cancel();
                    }
                }
            }
            catch (OperationCanceledException) when (sawText && stop.IsCancellationRequested && !token.IsCancellationRequested)
            {
                Require(timer.Elapsed < TimeSpan.FromSeconds(5), "Cancellation took more than five seconds.");
                return new("cancellation", "Passed", $"Cancelled active stream after text; observed in {timer.ElapsedMilliseconds} ms. Usage unavailable for incomplete run.", null);
            }
            Require(sawText && stop.IsCancellationRequested && !token.IsCancellationRequested
                && timer.Elapsed < TimeSpan.FromSeconds(5), "The active stream did not stop promptly after cancellation.");
            return new("cancellation", "Passed", $"Active stream ended after cancellation in {timer.ElapsedMilliseconds} ms (graceful completion). Usage unavailable for incomplete run.", null);
        });
        return results;

        async Task ProbeAsync(string name, Func<CancellationToken, Task<CapabilityResult>> probe)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromMinutes(2));
            try { results.Add(await probe(deadline.Token)); }
            catch (Exception error) when (!cancellationToken.IsCancellationRequested)
            {
                results.Add(new(name, "Failed", error.GetType().Name, null));
            }
        }
    }

    private AIAgent CreateAgent(string name, string? instructions = null,
        IList<AITool>? tools = null, ChatResponseFormat? schema = null)
        => client.AsAIAgent(new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new()
            {
                Instructions = instructions,
                Tools = tools,
                ResponseFormat = schema,
                MaxOutputTokens = 1024,
                Reasoning = disableReasoning ? new() { Effort = ReasoningEffort.None } : null
            }
        });

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
