using System.Text.Json;

namespace MafPromptJudgeDemo;

/// <summary>Validates evaluator fixtures before either simulation or inference.</summary>
public static class EvaluationSuite
{
    /// <summary>Rejects incomplete, inconsistent and duplicate evaluation cases.</summary>
    public static void Validate(IReadOnlyList<EvaluationCase> cases)
    {
        if (cases.Count == 0 || cases.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != cases.Count)
            throw new ArgumentException("A nonempty suite with unique case IDs is required.");
        foreach (var test in cases)
        {
            if (string.IsNullOrWhiteSpace(test.Id) || test.Input is null ||
                string.IsNullOrWhiteSpace(test.Input.UserInput) || test.Input.Context is null ||
                string.IsNullOrWhiteSpace(test.RequiredBehavior) || string.IsNullOrWhiteSpace(test.ForbiddenBehavior) ||
                test.Reference is null || test.Counterexample is null)
                throw new ArgumentException("Incomplete evaluation case.");
            var reference = Contracts.ParseAnswer(JsonSerializer.Serialize(test.Reference, Contracts.Json));
            Contracts.ParseAnswer(JsonSerializer.Serialize(test.Counterexample, Contracts.Json));
            if (reference.Action != test.ExpectedAction || reference.Refused != test.MustRefuse ||
                (test.MustRefuse && test.ExpectedAction != "refuse") || test.Reference == test.Counterexample)
                throw new ArgumentException("Inconsistent reference or counterexample.");
        }
    }
}

