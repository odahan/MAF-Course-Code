using Maf.Durable;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.Durable.Tests;

/// <summary>Verifies real rows, decision binding, transaction rollback and native checkpoint restoration.</summary>
[TestClass]
public sealed class ApprovalTests
{
    private string directory = null!;
    private string Database => Path.Combine(directory, "business.db");
    private ApprovalStore Store => new(Database);
    private static ApprovalDecision Decision(string choice = "approve") => new("operation-1", choice, "Reviewed by the business.");

    [TestInitialize]
    public void Initialize()
    {
        directory = Path.Combine(Path.GetTempPath(), "maf-durable-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Store.Register(ApprovalWorkflow.Example("operation-1"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(directory, true);
    }

    [TestMethod]
    public void ApprovalCreatesOneRealRowAcrossNewStoreInstances()
    {
        var first = Store.Apply(Decision());
        var replay = Store.Apply(Decision());
        Assert.AreEqual("Completed", replay.Status);
        Assert.AreEqual(first.ItemId, replay.ItemId);
        Assert.AreEqual(1L, RowCount());
        Assert.AreEqual("Regional pilot", replay.Draft.Title);
        Assert.AreEqual("Regional pilot", replay.StoredItem?.Title);
    }

    [TestMethod]
    public void RefusalAndRepeatedRefusalCreateNoBusinessRow()
    {
        Store.Apply(Decision("reject"));
        Assert.AreEqual("Rejected", Store.Apply(Decision("reject")).Status);
        Assert.AreEqual(0L, RowCount());
    }

    [TestMethod]
    public void InvalidDecisionLeavesTheOperationWaiting()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Store.RecordDecision(Decision("maybe")));
        Assert.ThrowsExactly<ArgumentException>(() => Store.RecordDecision(Decision() with { Comment = "" }));
        Assert.AreEqual("Waiting", Store.Read("operation-1").Status);
        Assert.IsNull(Store.Read("operation-1").Decision);
        Assert.AreEqual(0L, RowCount());
    }

    [TestMethod]
    public void UnknownOperationCannotAuthorizeAWrite()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Store.Apply(Decision() with { OperationId = "unknown" }));
        Assert.AreEqual(0L, RowCount());
    }

    [TestMethod]
    public void FirstDecisionCannotBeReplacedOrEdited()
    {
        Store.RecordDecision(Decision("reject"));
        Assert.ThrowsExactly<InvalidOperationException>(() => Store.Apply(Decision()));
        Assert.ThrowsExactly<InvalidOperationException>(() => Store.Apply(Decision("reject") with { Comment = "Changed" }));
        Assert.AreEqual(0L, RowCount());
    }

    [TestMethod]
    public void OperationKeyCannotBeReboundToAnotherDraft()
    {
        var original = ApprovalWorkflow.Example("operation-1");
        Store.Register(original);
        Assert.ThrowsExactly<InvalidOperationException>(() => Store.Register(original with
        {
            Draft = original.Draft with { Title = "Different work" }
        }));
    }

    [TestMethod]
    public void FailedBusinessInsertRollsBackCompletionAndCanBeRetried()
    {
        using (var connection = new SqliteConnection($"Data Source={Database}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TRIGGER FailInsert BEFORE INSERT ON ApprovedBacklogItems BEGIN SELECT RAISE(ABORT,'Injected failure'); END;";
            command.ExecuteNonQuery();
        }
        Assert.ThrowsExactly<SqliteException>(() => Store.Apply(Decision()));
        Assert.AreEqual("Approved", Store.Read("operation-1").Status);
        Assert.AreEqual(0L, RowCount());
        using (var connection = new SqliteConnection($"Data Source={Database}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DROP TRIGGER FailInsert";
            command.ExecuteNonQuery();
        }
        Assert.AreEqual("Completed", Store.Apply(Decision()).Status);
        Assert.AreEqual(1L, RowCount());
    }

    [TestMethod]
    public async Task ConcurrentDuplicateEffectsStillCreateOneRow()
    {
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Store.Apply(Decision()))));
        Assert.AreEqual(1L, RowCount());
    }

    [TestMethod]
    public async Task FileCheckpointReemitsTheSameRequestAndReplaysWithoutDuplicate()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        WaitingReceipt receipt;
        using (var files = new FileSystemJsonCheckpointStore(new DirectoryInfo(Path.Combine(directory, "checkpoints"))))
        {
            var manager = CheckpointManager.CreateJson(files);
            await using var run = await InProcessExecution.RunStreamingAsync(ApprovalWorkflow.Build(Store),
                ApprovalWorkflow.Example("operation-1"), manager, cancellationToken: timeout.Token);
            receipt = await ApprovalDemo.CaptureWaitingAsync(run, "operation-1", timeout.Token);
        }
        Assert.AreEqual("Waiting", Store.Read("operation-1").Status);
        for (var replay = 0; replay < 2; replay++)
        {
            using var files = new FileSystemJsonCheckpointStore(new DirectoryInfo(Path.Combine(directory, "checkpoints")));
            await using var run = await InProcessExecution.ResumeStreamingAsync(ApprovalWorkflow.Build(Store),
                new CheckpointInfo(receipt.SessionId, receipt.CheckpointId), CheckpointManager.CreateJson(files), timeout.Token);
            await ApprovalDemo.RespondAsync(run, Store, "operation-1", receipt.RequestId, Decision(), timeout.Token);
        }
        Assert.AreEqual(1L, RowCount());
    }

    [TestMethod]
    public async Task WrongResponseCorrelationDoesNotRecordADecision()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await using var run = await InProcessExecution.RunStreamingAsync(ApprovalWorkflow.Build(Store),
            ApprovalWorkflow.Example("operation-1"), cancellationToken: timeout.Token);
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => ApprovalDemo.RespondAsync(run, Store,
            "operation-1", null, Decision() with { OperationId = "other" }, timeout.Token));
        Assert.AreEqual("Waiting", Store.Read("operation-1").Status);
        Assert.AreEqual(0L, RowCount());
    }

    private long RowCount()
    {
        using var connection = new SqliteConnection($"Data Source={Database}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM ApprovedBacklogItems";
        return (long)command.ExecuteScalar()!;
    }
}
