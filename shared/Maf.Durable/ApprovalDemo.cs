using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace Maf.Durable;

/// <summary>Stores the exact waiting boundary independently of later completion checkpoints.</summary>
public sealed record WaitingReceipt(string GraphVersion, string SessionId, string CheckpointId,
    string OperationId, string RequestId);

/// <summary>Hosts typed requests, in-memory restoration and durable two-process approval.</summary>
public static class ApprovalDemo
{
    /// <summary>Runs a deterministic console host without requiring a model or credentials.</summary>
    public static async Task<int> RunAsync(string mode, string[] args)
    {
        try
        {
            if (mode != "durable")
            {
                await RunMemoryAsync(mode == "checkpoint", args.FirstOrDefault() ?? "approve");
                return 0;
            }

            if (args.Length < 2)
                throw new ArgumentException("Usage: start|resume|status <directory> [approve|reject] [comment] [--fail-after-write]");
            var directory = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(directory);
            using var lease = new FileStream(Path.Combine(directory, "host.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
            var store = new ApprovalStore(Path.Combine(directory, "business.db"));
            var receiptPath = Path.Combine(directory, "waiting.json");
            using var checkpoints = new FileSystemJsonCheckpointStore(new DirectoryInfo(Path.Combine(directory, "checkpoints")));
            var manager = CheckpointManager.CreateJson(checkpoints);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            if (args[0] == "start")
            {
                if (File.Exists(receiptPath)) throw new InvalidOperationException("This directory already contains an operation.");
                var ticket = ApprovalWorkflow.Example(Guid.NewGuid().ToString("N"));
                await using var run = await InProcessExecution.RunStreamingAsync(ApprovalWorkflow.Build(store), ticket,
                    manager, cancellationToken: timeout.Token);
                var receipt = await CaptureWaitingAsync(run, ticket.OperationId, timeout.Token);
                var temporary = receiptPath + ".tmp";
                File.WriteAllText(temporary, JsonSerializer.Serialize(receipt));
                File.Move(temporary, receiptPath, overwrite: true);
                Console.WriteLine("Waiting: " + JsonSerializer.Serialize(receipt));
                PrintState(store.Read(ticket.OperationId));
                return 0;
            }

            var saved = JsonSerializer.Deserialize<WaitingReceipt>(File.ReadAllText(receiptPath))!;
            if (saved.GraphVersion != ApprovalWorkflow.Version) throw new InvalidOperationException("Unsupported graph version.");
            if (args[0] == "status")
            {
                PrintState(store.Read(saved.OperationId));
                return 0;
            }
            if (args[0] != "resume" || args.Length < 3) throw new ArgumentException("A resume decision is required.");
            var decision = new ApprovalDecision(saved.OperationId, args[2], args.Length > 3 ? args[3] : "External business decision.");
            await using var resumed = await InProcessExecution.ResumeStreamingAsync(
                ApprovalWorkflow.Build(store, args.Contains("--fail-after-write")),
                new CheckpointInfo(saved.SessionId, saved.CheckpointId), manager, timeout.Token);
            await RespondAsync(resumed, store, saved.OperationId, saved.RequestId, decision, timeout.Token);
            PrintState(store.Read(saved.OperationId));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    /// <summary>Captures only a completed superstep that already contains the observed pending request.</summary>
    public static async Task<WaitingReceipt> CaptureWaitingAsync(StreamingRun run, string operationId,
        CancellationToken cancellationToken = default)
    {
        string? requestId = null;
        await foreach (var evt in run.WatchStreamAsync(cancellationToken))
        {
            Console.WriteLine("Event: " + evt.GetType().Name);
            if (evt is WorkflowErrorEvent error) throw new InvalidOperationException("Workflow failed.", error.Exception);
            if (evt is RequestInfoEvent request)
            {
                var ticket = request.Request.Data.As<ApprovalTicket>();
                if (ticket?.OperationId != operationId) throw new InvalidOperationException("Unexpected pending operation.");
                requestId = request.Request.RequestId;
            }
            if (requestId is not null && evt is SuperStepCompletedEvent { CompletionInfo.Checkpoint: { } checkpoint })
                return new(ApprovalWorkflow.Version, checkpoint.SessionId, checkpoint.CheckpointId, operationId, requestId);
        }
        throw new InvalidOperationException("No checkpoint containing the pending request was observed.");
    }

    /// <summary>Checks both engine request identity and business correlation before recording or sending a decision.</summary>
    public static async Task RespondAsync(StreamingRun run, ApprovalStore store, string operationId,
        string? requestId, ApprovalDecision decision, CancellationToken cancellationToken = default)
    {
        var completed = false;
        await foreach (var evt in run.WatchStreamAsync(cancellationToken))
        {
            if (evt is ExecutorInvokedEvent invoked) Console.WriteLine("Executor: " + invoked.ExecutorId);
            if (evt is WorkflowErrorEvent error) throw new InvalidOperationException("Workflow failed.", error.Exception);
            if (evt is RequestInfoEvent request)
            {
                var ticket = request.Request.Data.As<ApprovalTicket>();
                if (ticket?.OperationId != operationId || decision.OperationId != operationId
                    || (requestId is not null && request.Request.RequestId != requestId))
                    throw new ArgumentException("The response does not match the pending request.");
                store.RecordDecision(decision);
                Console.WriteLine("Responding: " + request.Request.RequestId);
                await run.SendResponseAsync(request.Request.CreateResponse(decision));
            }
            if (evt is WorkflowOutputEvent { Data: ApprovalState state })
            {
                PrintState(state);
                completed = true;
            }
        }
        if (!completed) throw new InvalidOperationException("The workflow ended without a business result.");
    }

    private static async Task RunMemoryAsync(bool restore, string choice)
    {
        var directory = Path.Combine(Path.GetTempPath(), "maf-memory-" + Guid.NewGuid().ToString("N"));
        var store = new ApprovalStore(Path.Combine(directory, "business.db"));
        var ticket = ApprovalWorkflow.Example(Guid.NewGuid().ToString("N"));
        var manager = CheckpointManager.CreateInMemory();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Console.WriteLine("In-memory engine state: restoration requires this live process. Business database: " + directory);
        var decision = new ApprovalDecision(ticket.OperationId, choice, "External business decision.");
        if (!restore)
        {
            await using var run = await InProcessExecution.RunStreamingAsync(ApprovalWorkflow.Build(store), ticket,
                cancellationToken: timeout.Token);
            await RespondAsync(run, store, ticket.OperationId, null, decision, timeout.Token);
            return;
        }
        WaitingReceipt receipt;
        await using (var run = await InProcessExecution.RunStreamingAsync(ApprovalWorkflow.Build(store), ticket,
            manager, cancellationToken: timeout.Token))
            receipt = await CaptureWaitingAsync(run, ticket.OperationId, timeout.Token);
        await using var resumed = await InProcessExecution.ResumeStreamingAsync(ApprovalWorkflow.Build(store),
            new CheckpointInfo(receipt.SessionId, receipt.CheckpointId), manager, timeout.Token);
        await RespondAsync(resumed, store, ticket.OperationId, receipt.RequestId, decision, timeout.Token);
    }

    private static void PrintState(ApprovalState state) => Console.WriteLine("Business state: " + JsonSerializer.Serialize(state));
}
