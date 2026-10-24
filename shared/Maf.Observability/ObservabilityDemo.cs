using System.Diagnostics;
using System.Text.Json;
using Maf.Backlog;
using Maf.Workflows;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Maf.Observability;

/// <summary>Runs a native MAF workflow, agent and local SQLite tool under one trace and durable chronology.</summary>
public static class ObservabilityDemo
{
    /// <summary>Supports explicit deterministic verification, injected failure and independent journal inspection.</summary>
    public static async Task<int> RunAsync(string[] args)
    {
        string path = Value(args, "--db") ?? Path.Combine(Path.GetTempPath(), "maf-observability.db");
        var journal = new BusinessJournal(path);
        if (Value(args, "--erase") is string erase)
        {
            Console.WriteLine($"Removed metadata rows: {journal.Erase(Guid.Parse(erase))}. Business actions remain committed.");
            return 0;
        }
        if (Value(args, "--read") is string read)
        {
            var entries = journal.Read(Guid.Parse(read));
            Console.WriteLine(JsonSerializer.Serialize(entries));
            return entries.Count > 0 && entries.Where(e => e.Kind == JournalKind.ActionCommitted)
                .All(e => e.ActionId.HasValue && journal.HasAction(e.ExecutionId, e.ActionId.Value)) ? 0 : 1;
        }
        using var provider = Telemetry.CreateProvider(Telemetry.Print);
        using var root = Telemetry.Source.StartActivity("execution");
        var id = Guid.NewGuid();
        journal.PurgeBefore(DateTimeOffset.UtcNow.AddDays(-30));
        Console.WriteLine($"ExecutionId={id:N}; TraceId={root?.TraceId}; Database={Path.GetFullPath(path)}");
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
        journal.Append(id, JournalKind.RequestReceived);
        try
        {
            bool deterministic = args.Contains("--deterministic");
            var configArgs = RemoveHostOptions(args);
            var settings = deterministic ? null : DemoSettings.Load(configArgs);
            Console.WriteLine(deterministic ? "Profile=deterministic; no model usage" : $"Profile={settings!.ChatProvider}; Model=" +
                (settings.ChatProvider == DemoConnection.ConnectionProvider.Ollama ? settings.LocalChat.Model : settings.ApiChat.Model));
            using var chat = deterministic ? null : new ObservedChatClient(DemoConnection.DemoConnection.CreateChatClient(settings),
                usage => Console.WriteLine("Usage per model request: " + JsonSerializer.Serialize(usage)));
            if (chat is not null)
            {
                int calls = 0;
                var policyTool = AIFunctionFactory.Create(() =>
                {
                    using var tool = Telemetry.Source.StartActivity("tool");
                    calls++;
                    journal.Append(id, JournalKind.Acknowledged);
                    tool?.SetStatus(ActivityStatusCode.Ok);
                    return "Review policy v1: only the host may commit a validated local review request.";
                }, name: "get_review_policy", description: "Reads the host review policy; performs no business write.");
                var policyAgent = Telemetry.Instrument(chat.AsAIAgent(name: "ReviewPolicyReader",
                    instructions: "Call get_review_policy before answering. Briefly acknowledge the returned policy in English.",
                    tools: [policyTool]));
                await policyAgent.RunAsync("Read the review policy using get_review_policy.", cancellationToken: stop.Token);
                if (calls == 0) throw new InvalidOperationException("The model did not invoke the required read tool.");
            }
            var reviewer = chat is null ? null : Telemetry.Instrument(BacklogWorkflow.CreateReviewer(chat));
            var workflow = BacklogWorkflow.Build(reviewer, telemetry: true);
            using (var span = Telemetry.Source.StartActivity("workflow"))
            {
                try
                {
                    await using var run = await InProcessExecution.RunStreamingAsync(workflow, BacklogWorkflow.Example, cancellationToken: stop.Token);
                    bool accepted = false;
                    await foreach (var evt in run.WatchStreamAsync(stop.Token))
                    {
                        Console.WriteLine("Workflow event: " + evt.GetType().Name);
                        if (evt is WorkflowErrorEvent) throw new InvalidOperationException("Workflow failed.");
                        if (evt is WorkflowOutputEvent { Data: ReviewResult result }) accepted = result.Status is "Ready" or "Reviewed";
                    }
                    if (!accepted) throw new InvalidOperationException("Review was not accepted.");
                    span?.SetStatus(ActivityStatusCode.Ok);
                }
                catch { span?.SetStatus(ActivityStatusCode.Error); throw; }
            }
            journal.Append(id, JournalKind.Intention);
            using (var tool = Telemetry.Source.StartActivity("tool"))
            {
                if (args.Contains("--fail"))
                {
                    tool?.SetStatus(ActivityStatusCode.Error);
                    throw new InvalidOperationException("Injected local tool failure.");
                }
                long action = journal.CommitReview(id);
                if (!journal.HasAction(id, action)) throw new InvalidOperationException("Missing committed action.");
                tool?.SetStatus(ActivityStatusCode.Ok);
            }
            journal.Append(id, JournalKind.ExecutionSucceeded);
            root?.SetStatus(ActivityStatusCode.Ok);
            Console.WriteLine("Journal: " + JsonSerializer.Serialize(journal.Read(id)));
            return 0;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            journal.Append(id, ex is OperationCanceledException ? JournalKind.ExecutionCancelled : JournalKind.ExecutionFailed);
            root?.SetStatus(ActivityStatusCode.Error);
            Console.Error.WriteLine(ex is OperationCanceledException ? "Execution cancelled." : "Execution failed; no generated fallback decision.");
            Console.WriteLine("Journal: " + JsonSerializer.Serialize(journal.Read(id)));
            return 1;
        }
    }

    private static string? Value(string[] args, string key)
    {
        int index = Array.IndexOf(args, key);
        if (index < 0) return null;
        if (index + 1 == args.Length) throw new ArgumentException("Missing option value.");
        return args[index + 1];
    }

    private static string[] RemoveHostOptions(string[] args)
    {
        var result = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--db" or "--read" or "--erase") { i++; continue; }
            if (args[i] is "--deterministic" or "--fail") continue;
            result.Add(args[i]);
        }
        return result.ToArray();
    }
}
