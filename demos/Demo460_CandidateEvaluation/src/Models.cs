using System.Text.Json;
using System.Text.Json.Serialization;

namespace MafPromptJudgeDemo;

/// <summary>Contains only information available to the deployed candidate.</summary>
public sealed record CandidateInput(string UserInput, string Context);

/// <summary>Stores evaluator-only expectations and authored reference answers.</summary>
public sealed record EvaluationCase(string Id, CandidateInput Input, bool Critical, bool MustRefuse,
    string ExpectedAction, string RequiredBehavior, string ForbiddenBehavior, CandidateAnswer Reference,
    CandidateAnswer Counterexample);

/// <summary>Defines the candidate contract; omitted refusal flags remain detectable.</summary>
public sealed record CandidateAnswer(string Action, string Response, bool? Refused);

/// <summary>Defines independent behavior judgments without a universal quality score.</summary>
public sealed record JudgeVerdict(bool? RequiredBehaviorMet, bool? ForbiddenBehaviorAbsent, string Explanation);

/// <summary>Retains raw observations, contract errors and failures for each attempt.</summary>
public sealed record EvaluationResult(string CaseId, int Repetition, bool Critical, string Status,
    string Stage, string? CandidateRaw, string? JudgeRaw, string Evidence, long ElapsedMilliseconds);

/// <summary>Defines strict serialization shared by the runner, tests and reports.</summary>
public static class Contracts
{
    /// <summary>Rejects unknown fields and preserves the exact contract names.</summary>
    public static JsonSerializerOptions Json { get; } = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
    };

    /// <summary>Rejects malformed, missing and oversized candidate fields without repairing them.</summary>
    public static CandidateAnswer ParseAnswer(string raw)
    {
        var answer = JsonSerializer.Deserialize<CandidateAnswer>(raw, Json)
            ?? throw new JsonException("Missing candidate answer.");
        if (answer.Action is not ("assist" or "refuse" or "clarify" or "abstain") ||
            string.IsNullOrWhiteSpace(answer.Response) || answer.Response.Length > 1200 || answer.Refused is null)
            throw new JsonException("Invalid candidate domain, response or missing refusal flag.");
        return answer;
    }

    /// <summary>Rejects incomplete judgments instead of supplying favorable defaults.</summary>
    public static JudgeVerdict ParseVerdict(string raw)
    {
        var verdict = JsonSerializer.Deserialize<JudgeVerdict>(raw, Json)
            ?? throw new JsonException("Missing judge verdict.");
        if (verdict.RequiredBehaviorMet is null || verdict.ForbiddenBehaviorAbsent is null ||
            string.IsNullOrWhiteSpace(verdict.Explanation) || verdict.Explanation.Length > 2000)
            throw new JsonException("Incomplete judge verdict.");
        return verdict;
    }
}

