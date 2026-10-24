using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace MafPromptJudgeDemo;

/// <summary>Runs a request without exposing evaluator state through its interface.</summary>
public interface IModelRunner
{
    /// <summary>Returns raw output for strict validation by the host.</summary>
    Task<string> RunAsync(string instructions, string input, bool judging, CancellationToken token);
}

/// <summary>Creates a fresh native MAF agent and session for every request.</summary>
public sealed class ModelRunner(IChatClient client) : IModelRunner
{
    /// <inheritdoc />
    public async Task<string> RunAsync(string instructions, string input, bool judging, CancellationToken token)
    {
        var options = new ChatOptions
        {
            Instructions = instructions,
            Temperature = 0,
            MaxOutputTokens = 800,
            ResponseFormat = judging ? ChatResponseFormat.ForJsonSchema<JudgeVerdict>(Contracts.Json)
                : ChatResponseFormat.ForJsonSchema<CandidateAnswer>(Contracts.Json)
        };
        var agent = client.AsAIAgent(new ChatClientAgentOptions
        {
            Name = judging ? "Evaluator" : "Candidate", ChatOptions = options
        });
        return (await agent.RunAsync(input, cancellationToken: token)).Text;
    }
}


