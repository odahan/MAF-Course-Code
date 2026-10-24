using System.Text.Json;
using MafPromptJudgeDemo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evaluation.Tests;

[TestClass]
public sealed class EvaluationTests
{
    private static EvaluationCase Case(bool critical = true) => new("PRIVATE-ID",
        new("Public request", "Public context"), critical, true, "refuse",
        "PRIVATE-REQUIRED", "PRIVATE-FORBIDDEN",
        new("refuse", "PRIVATE-REFERENCE", true), new("assist", "PRIVATE-COUNTEREXAMPLE", false));

    [TestMethod]
    public async Task CandidateReceivesNoPrivateExpectationsAndJudgeGetsOriginalAnswer()
    {
        var candidate = new RecordingRunner("""{"Action":"refuse","Response":"Unmodified answer.","Refused":true}""");
        var judge = new RecordingRunner("""{"RequiredBehaviorMet":true,"ForbiddenBehaviorAbsent":true,"Explanation":"Matches."}""");
        var result = await new PromptEvaluator(candidate, judge).RunAsync([Case()], "Candidate system", "Judge system", 1, default);
        Assert.IsTrue(PromptEvaluator.Accepted(result, 1));
        Assert.AreEqual(PromptEvaluator.BuildCandidatePrompt(Case().Input), candidate.LastInput);
        Assert.IsFalse(candidate.LastInput.Contains("PRIVATE", StringComparison.Ordinal));
        StringAssert.Contains(judge.LastInput, "PRIVATE-REQUIRED");
        StringAssert.Contains(judge.LastInput, "Unmodified answer.");
        Assert.IsFalse(judge.LastInput.Contains("PRIVATE-REFERENCE", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("{}")]
    [DataRow("null")]
    [DataRow("not json")]
    [DataRow("{\"Action\":\"assist\",\"Response\":\"ok\"}")]
    [DataRow("{\"Action\":\"delete\",\"Response\":\"ok\",\"Refused\":false}")]
    [DataRow("{\"Action\":\"assist\",\"Response\":\"\",\"Refused\":false}")]
    [DataRow("{\"Action\":\"assist\",\"Response\":\"ok\",\"Refused\":false,\"Extra\":true}")]
    public void CandidateContractRejectsInvalidOutput(string raw) =>
        Assert.ThrowsExactly<JsonException>(() => Contracts.ParseAnswer(raw));

    [TestMethod]
    [DataRow("{}")]
    [DataRow("null")]
    [DataRow("{\"RequiredBehaviorMet\":true,\"Explanation\":\"ok\"}")]
    [DataRow("{\"RequiredBehaviorMet\":true,\"ForbiddenBehaviorAbsent\":true,\"Explanation\":\"\"}")]
    [DataRow("{\"RequiredBehaviorMet\":5,\"ForbiddenBehaviorAbsent\":true,\"Explanation\":\"ok\"}")]
    public void JudgeContractRejectsInvalidOutput(string raw) =>
        Assert.ThrowsExactly<JsonException>(() => Contracts.ParseVerdict(raw));

    [TestMethod]
    public void EachBehaviorAndRefusalFlagBlocksIndependently()
    {
        var good = new JudgeVerdict(true, true, "Matches.");
        var answer = Case().Reference;
        Assert.IsTrue(PromptEvaluator.Passes(Case(), answer, good));
        Assert.IsFalse(PromptEvaluator.Passes(Case(), answer with { Refused = false }, good));
        Assert.IsFalse(PromptEvaluator.Passes(Case(), answer with { Action = "assist" }, good));
        Assert.IsFalse(PromptEvaluator.Passes(Case(), answer, good with { RequiredBehaviorMet = false }));
        Assert.IsFalse(PromptEvaluator.Passes(Case(), answer, good with { ForbiddenBehaviorAbsent = false }));
    }

    [TestMethod]
    public void CriticalFailureAndMissingResultsCannotBeHiddenByOtherPasses()
    {
        var passed = new EvaluationResult("ordinary", 1, false, "Passed", "judge", null, null, "ok", 0);
        var failed = passed with { CaseId = "critical", Critical = true, Status = "ContractError" };
        Assert.IsFalse(PromptEvaluator.Accepted([passed, passed, passed, failed], 4));
        Assert.IsFalse(PromptEvaluator.Accepted([passed], 2));
        Assert.IsFalse(PromptEvaluator.Accepted([], 0));
    }

    [TestMethod]
    public async Task InvalidCandidateNeverReachesJudgeAndOtherCasesContinue()
    {
        var candidate = new RecordingRunner("{}");
        var judge = new RecordingRunner("{}");
        var result = await new PromptEvaluator(candidate, judge).RunAsync([Case(), Case(false) with { Id = "second" }],
            "c", "j", 2, default);
        Assert.AreEqual(4, result.Count);
        Assert.IsTrue(result.All(r => r.Status == "ContractError" && r.Stage == "candidate"));
        Assert.AreEqual(0, judge.Calls);
    }

    [TestMethod]
    public async Task InvalidJudgeRemainsContractError()
    {
        var result = await new PromptEvaluator(
            new RecordingRunner(JsonSerializer.Serialize(Case().Reference)), new RecordingRunner("{}"))
            .RunAsync([Case()], "c", "j", 1, default);
        Assert.AreEqual("ContractError", result.Single().Status);
        Assert.AreEqual("judge", result.Single().Stage);
        Assert.IsFalse(PromptEvaluator.Accepted(result, 1));
    }

    [TestMethod]
    public async Task CancellationRemainsExecutionFailureAndIncompleteRunIsBlocked()
    {
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        var result = await new PromptEvaluator(new RecordingRunner("{}"), new RecordingRunner("{}"))
            .RunAsync([Case()], "c", "j", 2, stop.Token);
        Assert.AreEqual("ExecutionError", result.Single().Status);
        Assert.IsFalse(PromptEvaluator.Accepted(result, 2));
    }

    [TestMethod]
    public async Task DuplicateCaseIdsAreRejected() =>
        await Assert.ThrowsExactlyAsync<ArgumentException>(() =>
            new PromptEvaluator(new RecordingRunner("{}"), new RecordingRunner("{}"))
                .RunAsync([Case(), Case()], "c", "j", 1, default));

    private sealed class RecordingRunner(string output) : IModelRunner
    {
        public string LastInput { get; private set; } = "";
        public int Calls { get; private set; }

        public Task<string> RunAsync(string instructions, string input, bool judging, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Calls++;
            LastInput = input;
            return Task.FromResult(output);
        }
    }
}

