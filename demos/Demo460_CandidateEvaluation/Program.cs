using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Maf.Backlog;
using Maf.DemoConnection;
using MafPromptJudgeDemo;

string Option(string name, string fallback)
{
    var index = Array.IndexOf(args, name);
    return index < 0 ? fallback : index + 1 < args.Length ? args[index + 1]
        : throw new ArgumentException("Missing value for " + name);
}

var mode = Option("--mode", "fixture");
if (mode is not ("fixture" or "live")) throw new ArgumentException("Mode must be fixture or live.");
var version = Option("--prompt", "v2");
if (version is not ("v1" or "v2")) throw new ArgumentException("Prompt must be v1 or v2.");
var repetitions = int.Parse(Option("--repeat", "1"), System.Globalization.CultureInfo.InvariantCulture);
if (repetitions is < 1 or > 10) throw new ArgumentException("Repeat must be 1-10.");
var output = Path.GetFullPath(Option("--output", Path.Combine(Environment.CurrentDirectory, "evaluation-runs", Guid.NewGuid().ToString("N"))));
if (Directory.Exists(output)) throw new IOException("Use a new output directory for each run.");
Directory.CreateDirectory(output);
var root = AppContext.BaseDirectory;
var casePath = Path.Combine(root, "evaluation", "cases.v1.json");
var candidatePath = Path.Combine(root, "prompts", $"prompt_candidat_{version}.txt");
var judgePath = Path.Combine(root, "prompts", "prompt_juge.txt");
var cases = JsonSerializer.Deserialize<EvaluationCase[]>(await File.ReadAllTextAsync(casePath), Contracts.Json)
    ?? throw new InvalidDataException("Missing suite.");
var candidatePrompt = await File.ReadAllTextAsync(candidatePath);
var judgePrompt = await File.ReadAllTextAsync(judgePath);
var settings = DemoSettings.Load(args);
var selected = settings.ChatProvider == ConnectionProvider.Ollama ? settings.LocalChat : settings.ApiChat;
var judgeModel = Option("--judge-model", selected.Model);
var judgeSettings = settings.ChatProvider == ConnectionProvider.Ollama
    ? settings with { LocalChat = selected with { Model = judgeModel } }
    : settings with { ApiChat = selected with { Model = judgeModel } };
var artifacts = new[] { casePath, candidatePath, judgePath, typeof(PromptEvaluator).Assembly.Location,
    typeof(DemoConnection).Assembly.Location, Path.Combine(root, "ai-settings.json") };
var manifest = artifacts.Select(path => new
{
    Name = Path.GetFileName(path),
    Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))
}).ToArray();
await File.WriteAllTextAsync(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(manifest, Contracts.Json));
File.Copy(casePath, Path.Combine(output, "cases.json"));
File.Copy(candidatePath, Path.Combine(output, "candidate.txt"));
File.Copy(judgePath, Path.Combine(output, "judge.txt"));
using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(20));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
var timer = Stopwatch.StartNew();
IReadOnlyList<EvaluationResult> results;
var calibrations = new List<object>();
var calibrationPassed = true;
if (mode == "fixture")
{
    var fixture = new FixtureRunner(cases);
    results = await new PromptEvaluator(fixture, fixture).RunAsync(cases, candidatePrompt, judgePrompt, repetitions, stop.Token);
}
else
{
    using var candidateClient = DemoConnection.CreateChatClient(settings);
    using var judgeClient = DemoConnection.CreateChatClient(judgeSettings);
    var candidate = new ModelRunner(candidateClient);
    var judge = new ModelRunner(judgeClient);
    foreach (var test in cases)
    {
        foreach (var positive in new[] { true, false })
        {
            string? raw = null;
            var status = "ExecutionError";
            try
            {
                raw = await judge.RunAsync(judgePrompt,
                    PromptEvaluator.BuildJudgePrompt(test, positive ? test.Reference : test.Counterexample), true, stop.Token);
                var verdict = Contracts.ParseVerdict(raw);
                var observed = verdict.RequiredBehaviorMet == true && verdict.ForbiddenBehaviorAbsent == true;
                status = observed == positive ? "Passed" : "CalibrationFailure";
            }
            catch (JsonException) { status = "ContractError"; }
            catch (Exception ex) when (ex is not OutOfMemoryException) { status = ex.GetType().Name; }
            calibrationPassed &= status == "Passed";
            calibrations.Add(new { test.Id, Positive = positive, Status = status, Raw = raw });
            Console.WriteLine($"Calibration {test.Id} positive={positive}: {status}");
        }
    }
    results = await new PromptEvaluator(candidate, judge).RunAsync(cases, candidatePrompt, judgePrompt, repetitions, stop.Token);
}
var accepted = PromptEvaluator.Accepted(results, cases.Length * repetitions) && calibrationPassed;
var report = new
{
    FormatVersion = 1, SuiteVersion = "support-behaviors-v1", CandidatePrompt = version, JudgePrompt = "v1",
    StartedUtc = DateTimeOffset.UtcNow - timer.Elapsed, DurationMilliseconds = timer.ElapsedMilliseconds,
    Mode = mode, Measurement = mode == "live" ? "ObservedModelOutputs" : "ScriptedFixtureOnly",
    Provider = mode == "live" ? settings.ChatProvider.ToString() : "None",
    Endpoint = mode == "live" ? selected.Endpoint : null,
    CandidateModel = mode == "live" ? selected.Model : null, JudgeModel = mode == "live" ? judgeModel : null,
    SharedModel = mode == "live" && selected.Model == judgeModel,
    Temperature = 0, MaxOutputTokens = 800, settings.TimeoutSeconds, Repetitions = repetitions,
    Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    MafVersion = typeof(Microsoft.Agents.AI.AIAgent).Assembly.GetName().Version?.ToString(),
    FixtureGatePassed = mode == "fixture" && accepted,
    ModelGatePassed = mode == "live" && accepted,
    CriticalFailures = results.Count(r => r.Critical && r.Status != "Passed"),
    ContractErrors = results.Count(r => r.Status == "ContractError"),
    CalibrationPassed = mode == "live" ? (bool?)calibrationPassed : null,
    Calibrations = calibrations, Results = results
};
await File.WriteAllTextAsync(Path.Combine(output, "report.json"), JsonSerializer.Serialize(report, Contracts.Json));
Console.WriteLine($"Mode={mode}; accepted={accepted}; results={results.Count}; report={output}");
return accepted ? 0 : 1;

/// <summary>Replays explicit authored fixtures; this is never a model-quality measurement.</summary>
internal sealed class FixtureRunner(IReadOnlyList<EvaluationCase> cases) : IModelRunner
{
    /// <inheritdoc />
    public Task<string> RunAsync(string instructions, string input, bool judging, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(judging
            ? JsonSerializer.Serialize(new JudgeVerdict(true, true, "Scripted positive fixture."), Contracts.Json)
            : JsonSerializer.Serialize(cases.Single(c => PromptEvaluator.BuildCandidatePrompt(c.Input) == input).Reference, Contracts.Json));
    }
}

