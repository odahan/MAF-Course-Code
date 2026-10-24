using System.Diagnostics;
using System.Text.Json;

namespace MafPromptJudgeDemo;

/// <summary>Separates candidate requests from private evaluator expectations by construction.</summary>
public sealed class PromptEvaluator(IModelRunner candidate, IModelRunner judge)
{
    /// <summary>Serializes only public input, without case IDs, labels, references or criteria.</summary>
    public static string BuildCandidatePrompt(CandidateInput input) => JsonSerializer.Serialize(input, Contracts.Json);

    /// <summary>Supplies expectations only to the judge; embedded text remains untrusted data.</summary>
    public static string BuildJudgePrompt(EvaluationCase test, CandidateAnswer answer) =>
        JsonSerializer.Serialize(new { test.Input, test.RequiredBehavior, test.ForbiddenBehavior, Answer = answer }, Contracts.Json);

    /// <summary>Requires every applicable behavior; a good average cannot bypass a failure.</summary>
    public static bool Passes(EvaluationCase test, CandidateAnswer answer, JudgeVerdict verdict) =>
        answer.Action == test.ExpectedAction && answer.Refused == test.MustRefuse &&
        verdict.RequiredBehaviorMet == true && verdict.ForbiddenBehaviorAbsent == true;

    /// <summary>Evaluates all cases and repetitions, retaining operational and contract failures separately.</summary>
    public async Task<IReadOnlyList<EvaluationResult>> RunAsync(IReadOnlyList<EvaluationCase> cases,
        string candidateInstructions, string judgeInstructions, int repetitions, CancellationToken token)
    {
        if (repetitions is < 1 or > 10 || cases.Count == 0 || cases.Select(c => c.Id).Distinct().Count() != cases.Count)
            throw new ArgumentException("Use a nonempty suite with unique IDs and 1-10 repetitions.");
        EvaluationSuite.Validate(cases);
        var results = new List<EvaluationResult>();
        for (var repetition = 1; repetition <= repetitions; repetition++)
        {
            foreach (var test in cases)
            {
                var timer = Stopwatch.StartNew();
                string? candidateRaw = null;
                string? judgeRaw = null;
                var stage = "candidate";
                try
                {
                    candidateRaw = await candidate.RunAsync(candidateInstructions, BuildCandidatePrompt(test.Input), false, token);
                    var answer = Contracts.ParseAnswer(candidateRaw);
                    stage = "judge";
                    judgeRaw = await judge.RunAsync(judgeInstructions, BuildJudgePrompt(test, answer), true, token);
                    var verdict = Contracts.ParseVerdict(judgeRaw);
                    results.Add(new(test.Id, repetition, test.Critical,
                        Passes(test, answer, verdict) ? "Passed" : "BehaviorFailure", stage,
                        candidateRaw, judgeRaw, verdict.Explanation, timer.ElapsedMilliseconds));
                }
                catch (JsonException)
                {
                    results.Add(new(test.Id, repetition, test.Critical, "ContractError", stage,
                        candidateRaw, judgeRaw, "Malformed JSON or invalid required fields.", timer.ElapsedMilliseconds));
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    results.Add(new(test.Id, repetition, test.Critical, "ExecutionError", stage,
                        candidateRaw, judgeRaw, ex.GetType().Name, timer.ElapsedMilliseconds));
                }
                if (token.IsCancellationRequested) return results;
            }
        }
        return results;
    }

    /// <summary>Blocks incomplete runs, contract/transport failures and every behavior regression.</summary>
    public static bool Accepted(IReadOnlyList<EvaluationResult> results, int expectedCount) =>
        expectedCount > 0 && results.Count == expectedCount && results.All(r => r.Status == "Passed");
}


