using System.Runtime.CompilerServices;
using System.Text.Json;
using MafPromptJudgeDemo;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evaluation.Tests;

[TestClass]
public sealed class ModelRunnerTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeMafSchemaUsesExactlyTheStrictReaderPropertyNames(bool judging)
    {
        using var client = new SchemaClient();
        var raw = await new ModelRunner(client).RunAsync("Return requested JSON.", "Public input.", judging, default);
        var format = client.Options?.ResponseFormat as ChatResponseFormatJson;
        Assert.IsNotNull(format);
        using var schema = JsonDocument.Parse(format.Schema!.Value.GetRawText());
        var properties = schema.RootElement.GetProperty("properties");
        Assert.IsTrue(properties.TryGetProperty(judging ? "RequiredBehaviorMet" : "Action", out _));
        if (judging) Assert.IsNotNull(Contracts.ParseVerdict(raw));
        else Assert.IsNotNull(Contracts.ParseAnswer(raw));
        Assert.AreEqual(0f, client.Options!.Temperature);
    }

    [TestMethod]
    public void ShippedSuiteHasValidReferencesAndFourCriticalCases()
    {
        var cases = JsonSerializer.Deserialize<EvaluationCase[]>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "evaluation", "cases.v1.json")), Contracts.Json)!;
        EvaluationSuite.Validate(cases);
        Assert.AreEqual(8, cases.Length);
        Assert.AreEqual(4, cases.Count(c => c.Critical));
    }

    [TestMethod]
    public void InconsistentFixtureCannotBecomeAPassingSimulation()
    {
        var input = new CandidateInput("Request", "");
        var test = new EvaluationCase("one", input, true, true, "refuse", "Required", "Forbidden",
            new("assist", "Incorrect reference.", false), new("assist", "Bad answer.", false));
        Assert.ThrowsExactly<ArgumentException>(() => EvaluationSuite.Validate([test]));
    }

    private sealed class SchemaClient : IChatClient
    {
        public ChatOptions? Options { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            var judging = ((ChatResponseFormatJson)options!.ResponseFormat!).Schema!.Value
                .GetProperty("properties").TryGetProperty("RequiredBehaviorMet", out _);
            var raw = judging
                ? JsonSerializer.Serialize(new JudgeVerdict(true, true, "Matches."), Contracts.Json)
                : JsonSerializer.Serialize(new CandidateAnswer("assist", "Advice.", false), Contracts.Json);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, raw)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }
}

