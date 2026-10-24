using System.Text.Json;
using Maf.Backlog;
using Microsoft.Data.Sqlite;

namespace Maf.Durable;

/// <summary>Correlates a stable business operation with the immutable draft awaiting approval.</summary>
public sealed record ApprovalTicket(string OperationId, BacklogDraft Draft);

/// <summary>Carries a correlated external decision; only approve and reject are accepted.</summary>
public sealed record ApprovalDecision(string OperationId, string Choice, string Comment);

/// <summary>Exposes persisted state and the actual business row, rather than an invocation count.</summary>
public sealed record ApprovalState(string OperationId, string Status, ApprovalDecision? Decision,
    long? ItemId, BacklogDraft Draft, BacklogDraft? StoredItem);

/// <summary>Coordinates first-decision-wins approval and a unique SQLite business effect.</summary>
public sealed class ApprovalStore(string path)
{
    private SqliteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Approvals (
                OperationId TEXT PRIMARY KEY, Draft TEXT NOT NULL, Decision TEXT,
                Status TEXT NOT NULL CHECK(Status IN ('Waiting','Approved','Rejected','Completed')));
            CREATE TABLE IF NOT EXISTS ApprovedBacklogItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, OperationId TEXT NOT NULL UNIQUE,
                Draft TEXT NOT NULL, CreatedUtc TEXT NOT NULL);
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    /// <summary>Registers a stable operation key; reusing it for another draft is forbidden.</summary>
    public void Register(ApprovalTicket ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket.OperationId) || ticket.Draft.Validate().Length != 0)
            throw new ArgumentException("A valid operation and actionable draft are required.");
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Approvals VALUES ($id,$draft,NULL,'Waiting') ON CONFLICT DO NOTHING";
        command.Parameters.AddWithValue("$id", ticket.OperationId);
        command.Parameters.AddWithValue("$draft", JsonSerializer.Serialize(ticket.Draft));
        command.ExecuteNonQuery();
        if (JsonSerializer.Serialize(Read(ticket.OperationId).Draft) != JsonSerializer.Serialize(ticket.Draft))
            throw new InvalidOperationException("The operation key is already bound to another draft.");
    }

    /// <summary>Validates before persisting; exact repetitions are accepted and conflicting decisions fail.</summary>
    public void RecordDecision(ApprovalDecision decision)
    {
        if (decision.Choice is not ("approve" or "reject") || string.IsNullOrWhiteSpace(decision.Comment)
            || decision.Comment.Length > 500)
            throw new ArgumentException("Use approve or reject and a comment of 1 to 500 characters.");
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Decision FROM Approvals WHERE OperationId=$id";
        command.Parameters.AddWithValue("$id", decision.OperationId);
        var existing = command.ExecuteScalar() ?? throw new ArgumentException("Unknown operation.");
        var json = JsonSerializer.Serialize(decision);
        if (existing is string stored && stored != json)
            throw new InvalidOperationException("A different decision has already been recorded.");
        if (existing is DBNull)
        {
            command.CommandText = "UPDATE Approvals SET Decision=$decision,Status=$status WHERE OperationId=$id";
            command.Parameters.AddWithValue("$decision", json);
            command.Parameters.AddWithValue("$status", decision.Choice == "approve" ? "Approved" : "Rejected");
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    /// <summary>Atomically creates the business row and completes the operation; replay returns the same row.</summary>
    public ApprovalState Apply(ApprovalDecision decision)
    {
        RecordDecision(decision);
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ApprovedBacklogItems (OperationId,Draft,CreatedUtc)
            SELECT OperationId,Draft,$utc FROM Approvals WHERE OperationId=$id AND Status IN ('Approved','Completed')
            ON CONFLICT(OperationId) DO NOTHING;
            UPDATE Approvals SET Status='Completed' WHERE OperationId=$id AND Status='Approved';
            """;
        command.Parameters.AddWithValue("$id", decision.OperationId);
        command.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
        transaction.Commit();
        return Read(decision.OperationId);
    }

    /// <summary>Reads the persisted approval and joined business item for independent verification.</summary>
    public ApprovalState Read(string operationId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT a.Status,a.Decision,b.Id,a.Draft,b.Draft FROM Approvals a
            LEFT JOIN ApprovedBacklogItems b ON b.OperationId=a.OperationId WHERE a.OperationId=$id
            """;
        command.Parameters.AddWithValue("$id", operationId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new ArgumentException("Unknown operation.");
        return new(operationId, reader.GetString(0), reader.IsDBNull(1) ? null :
            JsonSerializer.Deserialize<ApprovalDecision>(reader.GetString(1)),
            reader.IsDBNull(2) ? null : reader.GetInt64(2),
            JsonSerializer.Deserialize<BacklogDraft>(reader.GetString(3))!,
            reader.IsDBNull(4) ? null : JsonSerializer.Deserialize<BacklogDraft>(reader.GetString(4)));
    }
}
