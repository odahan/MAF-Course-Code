using System.Diagnostics;
using System.Text.Json;
using Maf.Backlog;
using Maf.DemoConnection;
using Microsoft.Agents.AI.Workflows;

namespace Maf.Workflows;

/// <summary>Runs the three Video2 entry points with bounded cancellation and optional execution evidence.</summary>
public static class WorkflowDemo
{
    /// <summary>Runs one fresh graph per request; native agent sessions live only for that graph run.</summary>
    public static async Task<int> RunAsync(string[] args, bool useAgent, bool observe)
    {
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
        var input = BacklogWorkflow.Example;
        if (args.Contains("--incomplete")) input = input with { ClarifyingQuestions = ["Which account is affected?"] };
        if (args.Contains("--invalid")) input = input with { Component = "Unknown" };
        var options = args.Where(a => a is not ("--incomplete" or "--invalid")).ToArray();
        using var client = useAgent ? DemoConnection.DemoConnection.CreateChatClient(DemoSettings.Load(options)) : null;
        Console.WriteLine("Planned graph: AnalyzeDraft -> ClarifyDraft OR " +
            (useAgent ? "PrepareReview -> BacklogReviewer (native MAF) -> ValidateReview" : "AcceptDraft"));
        Console.WriteLine("Input: " + JsonSerializer.Serialize(input));
        var activities = new List<object>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => observe && source.Name.StartsWith("Microsoft.Agents.AI", StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (activities) activities.Add(new { Source = activity.Source.Name, activity.OperationName,
                    TraceId = activity.TraceId.ToString(), SpanId = activity.SpanId.ToString(),
                    ParentSpanId = activity.ParentSpanId.ToString(), DurationMs = activity.Duration.TotalMilliseconds });
            }
        };
        ActivitySource.AddActivityListener(listener);
        var workflow = BacklogWorkflow.Build(client is null ? null : BacklogWorkflow.CreateReviewer(client), telemetry: observe);
        var path = new List<string>();
        var eventNames = new List<string>();
        var results = new List<ReviewResult>();
        var errors = new List<string>();
        try
        {
            await using var run = await InProcessExecution.RunStreamingAsync(workflow, input, cancellationToken: stop.Token);
            await foreach (var evt in run.WatchStreamAsync(stop.Token))
            {
                eventNames.Add(evt.GetType().Name);
                if (observe) Console.WriteLine("Event: " + evt.GetType().Name);
                if (evt is ExecutorInvokedEvent invoked) path.Add(invoked.ExecutorId);
                if (evt is WorkflowErrorEvent error) errors.Add(error.Exception?.Message ?? "Unknown workflow error.");
                if (evt is WorkflowOutputEvent { Data: ReviewResult result }) results.Add(result);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            errors.Add(ex.Message);
        }
        Console.WriteLine("Executed path: " + string.Join(" -> ", path));
        foreach (var result in results) Console.WriteLine("Business result: " + JsonSerializer.Serialize(result));
        foreach (var error in errors) Console.Error.WriteLine("Workflow failed: " + error);
        if (observe)
        {
            Console.WriteLine("Telemetry spans (distinct from workflow events): " + JsonSerializer.Serialize(activities));
            Console.WriteLine("Evidence: " + JsonSerializer.Serialize(new { path, eventNames, results, errors, activities }));
        }
        return errors.Count == 0 && results.Count == 1 && results[0].Status != "Rejected" ? 0 : 1;
    }
}
