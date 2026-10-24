using System.Diagnostics;
using Microsoft.Data.Sqlite;

namespace Maf.Observability;

/// <summary>Separates host intention, acknowledgement and a transactionally committed local action.</summary>
public enum JournalKind
{
    /// <summary>The host accepted an execution.</summary>
    RequestReceived,
    /// <summary>The host intends to perform an action.</summary>
    Intention,
    /// <summary>A tool acknowledged a read or request; no business write is proven.</summary>
    Acknowledged,
    /// <summary>A local business row and this event committed in the same transaction.</summary>
    ActionCommitted,
    /// <summary>The host completed the scenario.</summary>
    ExecutionSucceeded,
    /// <summary>The scenario failed; earlier committed actions are not undone.</summary>
    ExecutionFailed,
    /// <summary>Cancellation interrupted the scenario.</summary>
    ExecutionCancelled
}

/// <summary>Contains only host-generated identifiers and closed enums; no prompts, arguments, answers or free exception text.</summary>
public sealed record JournalEntry(long Id, Guid ExecutionId, DateTimeOffset Utc, string TraceId, JournalKind Kind, long? ActionId);

/// <summary>Stores a minimal EP008 chronology and a demonstrable SQLite business effect in one database.</summary>
public sealed class BusinessJournal(string path)
{
    private SqliteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS AuditEvents (
                Id INTEGER PRIMARY KEY, ExecutionId TEXT NOT NULL, Utc TEXT NOT NULL,
                TraceId TEXT NOT NULL, Kind TEXT NOT NULL, ActionId INTEGER);
            CREATE INDEX IF NOT EXISTS IX_AuditEvents_Execution ON AuditEvents(ExecutionId);
            CREATE TABLE IF NOT EXISTS RecordedDecisions (
                Id INTEGER PRIMARY KEY, ExecutionId TEXT NOT NULL UNIQUE, Decision TEXT NOT NULL CHECK(Decision='ReviewRequested'));
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    /// <summary>Appends a host event. ActionCommitted can only be written by the transactional method.</summary>
    public void Append(Guid executionId, JournalKind kind)
    {
        if (!Enum.IsDefined(kind) || kind == JournalKind.ActionCommitted) throw new ArgumentException("Unsupported event.");
        using var connection = Open();
        Insert(connection, null, executionId, kind, null);
    }

    /// <summary>Commits a local review request and its evidence atomically; replay returns the same action without another event.</summary>
    public long CommitReview(Guid executionId)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO RecordedDecisions(ExecutionId,Decision) VALUES($id,'ReviewRequested') ON CONFLICT DO NOTHING";
        command.Parameters.AddWithValue("$id", executionId.ToString("N"));
        bool created = command.ExecuteNonQuery() == 1;
        command.CommandText = "SELECT Id FROM RecordedDecisions WHERE ExecutionId=$id";
        long id = (long)command.ExecuteScalar()!;
        if (created) Insert(connection, transaction, executionId, JournalKind.ActionCommitted, id);
        transaction.Commit();
        return id;
    }

    private static void Insert(SqliteConnection connection, SqliteTransaction? transaction, Guid executionId, JournalKind kind, long? actionId)
    {
        if (executionId == Guid.Empty) throw new ArgumentException("An execution identifier is required.");
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO AuditEvents(ExecutionId,Utc,TraceId,Kind,ActionId) VALUES($id,$utc,$trace,$kind,$action)";
        command.Parameters.AddWithValue("$id", executionId.ToString("N"));
        command.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$trace", Activity.Current?.TraceId.ToString() ?? "");
        command.Parameters.AddWithValue("$kind", kind.ToString());
        command.Parameters.AddWithValue("$action", (object?)actionId ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    /// <summary>Reads one chronological execution, including the joined proof identifier.</summary>
    public IReadOnlyList<JournalEntry> Read(Guid executionId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id,Utc,TraceId,Kind,ActionId FROM AuditEvents WHERE ExecutionId=$id ORDER BY Id";
        command.Parameters.AddWithValue("$id", executionId.ToString("N"));
        using var reader = command.ExecuteReader();
        var entries = new List<JournalEntry>();
        while (reader.Read()) entries.Add(new(reader.GetInt64(0), executionId, DateTimeOffset.Parse(reader.GetString(1)),
            reader.GetString(2), Enum.Parse<JournalKind>(reader.GetString(3)), reader.IsDBNull(4) ? null : reader.GetInt64(4)));
        return entries;
    }

    /// <summary>Independently verifies that the journal points to a committed row in this local database.</summary>
    public bool HasAction(Guid executionId, long actionId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM RecordedDecisions WHERE ExecutionId=$id AND Id=$action";
        command.Parameters.AddWithValue("$id", executionId.ToString("N"));
        command.Parameters.AddWithValue("$action", actionId);
        return (long)command.ExecuteScalar()! == 1;
    }

    /// <summary>Deletes expired metadata; the caller chooses an explicit retention cutoff. Business rows remain separate.</summary>
    public int PurgeBefore(DateTimeOffset cutoff)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM AuditEvents WHERE Utc < $utc";
        command.Parameters.AddWithValue("$utc", cutoff.ToUniversalTime().ToString("O"));
        return command.ExecuteNonQuery();
    }

    /// <summary>Erases one execution's metadata; this is not an undo of a committed business action.</summary>
    public int Erase(Guid executionId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM AuditEvents WHERE ExecutionId=$id";
        command.Parameters.AddWithValue("$id", executionId.ToString("N"));
        return command.ExecuteNonQuery();
    }
}
