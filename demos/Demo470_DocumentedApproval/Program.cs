using System.Diagnostics;
using System.Text.Json;
using Maf.Backlog;
using Maf.Durable;
using Maf.Observability;
using Maf.Rag;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;

return await DocumentedApproval.RunAsync(args);

/// <summary>Links governed evidence to an immutable draft and a durable native approval workflow.</summary>
internal static class DocumentedApproval
{
    /// <summary>Runs one bounded phase; approval always requires a separate explicit host command.</summary>
    public static async Task<int> RunAsync(string[] args)
    {
        using var telemetry = Telemetry.CreateProvider(Telemetry.Print);
        using var activity = Telemetry.Source.StartActivity("execution");
        try
        {
            if (args.Length < 2 || args[0] is not ("start" or "resume" or "status"))
                throw new ArgumentException("Usage: start <new-directory> [--live|--invalid-evidence] | resume <directory> approve|reject | status <directory>");
            string directory = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(directory);
            using var lease = new FileStream(Path.Combine(directory, "host.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
            string receiptPath = Path.Combine(directory, "waiting.json");
            var store = new ApprovalStore(Path.Combine(directory, "business.db"));
            using var checkpoints = new FileSystemJsonCheckpointStore(new DirectoryInfo(Path.Combine(directory, "checkpoints")));
            var manager = CheckpointManager.CreateJson(checkpoints);
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            if (args[0] == "start")
            {
                if (File.Exists(receiptPath) || File.Exists(Path.Combine(directory, "evidence.json")))
                    throw new InvalidOperationException("Use a new directory for each proposal.");
                const string question = "Where can I export CSV in Reporting?";
                var asOf = new DateOnly(2026, 10, 23);
                var corpus = Corpus.Load(Path.Combine(AppContext.BaseDirectory, "corpus.json"));
                var vectors = new SqliteVectorStore(Path.Combine(directory, "vectors.db"));
                ControlledAnswer answer;
                if (args.Contains("--live"))
                {
                    var settings = DemoSettings.Load([]);
                    Console.WriteLine($"Profile=live; Chat={settings.ChatProvider}; Embeddings={settings.Embeddings.Model}");
                    using var index = new EmbeddingIndex(settings, corpus, vectors);
                    await index.RebuildAsync(timeout.Token);
                    var retrieval = await index.RetrieveAsync(question, asOf, "course", ct: timeout.Token);
                    answer = await RagAnswerService.AnswerAsync(settings, retrieval, "context", timeout.Token);
                }
                else
                {
                    Console.WriteLine("Profile=fixture; controlled dense vectors and answer; no model qualification");
                    var manifest = IndexManifest.Create(corpus, "fixture", "fixture", "controlled-2d-v1", 2);
                    var chunks = CorpusPolicy.Chunks(corpus);
                    vectors.Rebuild(corpus, manifest, chunks.Select(c => c.Source.Id == "ACME-EXPORT"
                        ? new float[] { 1, 0 } : new float[] { 0, 1 }).ToArray());
                    var hits = vectors.Search(corpus, manifest, [1, 0], asOf, "course");
                    var selected = hits.Single(h => h.Chunk.Source.Id == "ACME-EXPORT");
                    answer = AnswerGate.Validate(new(true, [new(args.Contains("--invalid-evidence")
                        ? "INVENTED#0" : selected.Chunk.Id, selected.Chunk.Text)]), new(question, asOf, hits, null));
                }
                if (!answer.Accepted) throw new InvalidOperationException(answer.Text);
                var ticket = new ApprovalTicket(Guid.NewGuid().ToString("N"), new BacklogDraft(
                    "Review Reporting CSV export", "Question", "medium", "Reporting", answer.Text, []));
                if (ticket.Draft.Validate().Length != 0) throw new InvalidOperationException("Evidence exceeds the business contract.");
                await using var run = await InProcessExecution.RunStreamingAsync(ApprovalWorkflow.Build(store), ticket,
                    manager, cancellationToken: timeout.Token);
                var receipt = await ApprovalDemo.CaptureWaitingAsync(run, ticket.OperationId, timeout.Token);
                File.WriteAllText(Path.Combine(directory, "evidence.json"), JsonSerializer.Serialize(answer));
                File.WriteAllText(receiptPath, JsonSerializer.Serialize(receipt));
                Console.WriteLine("Waiting: " + JsonSerializer.Serialize(receipt));
                Print(store.Read(ticket.OperationId));
            }
            else
            {
                var saved = JsonSerializer.Deserialize<WaitingReceipt>(File.ReadAllText(receiptPath))!;
                if (saved.GraphVersion != ApprovalWorkflow.Version) throw new InvalidOperationException("Graph version mismatch.");
                if (args[0] == "resume")
                {
                    if (args.Length != 3 || args[2] is not ("approve" or "reject"))
                        throw new ArgumentException("An explicit approve or reject decision is required.");
                    var decision = new ApprovalDecision(saved.OperationId, args[2], "External business decision.");
                    await using var run = await InProcessExecution.ResumeStreamingAsync(ApprovalWorkflow.Build(store),
                        new CheckpointInfo(saved.SessionId, saved.CheckpointId), manager, timeout.Token);
                    await ApprovalDemo.RespondAsync(run, store, saved.OperationId, saved.RequestId, decision, timeout.Token);
                }
                Print(store.Read(saved.OperationId));
            }
            activity?.SetStatus(ActivityStatusCode.Ok);
            return 0;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static void Print(ApprovalState state) => Console.WriteLine("Business state: " + JsonSerializer.Serialize(state));
}
