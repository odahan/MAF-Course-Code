using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Data.Sqlite;

namespace Maf.Memory;

/// <summary>Resolves state independently of the current working directory.</summary>
public static class StatePaths
{
    /// <summary>Accepts an absolute override, otherwise uses a per-demo local application directory.</summary>
    public static string Resolve(string demo, string fileName)
    {
        string root = global::Maf.DemoConnection.DemoConfiguration.GetSetting("MAF_STATE_ROOT")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MafBaseTechnique");
        if (!Path.IsPathFullyQualified(root))
            throw new ArgumentException("MAF_STATE_ROOT must be an absolute path.");
        string path = Path.Combine(root, demo, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }
}

/// <summary>Stores the explicit product context; initialization never removes existing state.</summary>
public sealed class ContextStore
{
    private readonly string _connectionString;

    /// <summary>Initializes a database at an explicit absolute path.</summary>
    public ContextStore(string path)
    {
        if (!Path.IsPathFullyQualified(path))
            throw new ArgumentException("Database path must be absolute.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        Execute("CREATE TABLE IF NOT EXISTS AgentState (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);");
    }

    /// <summary>Reads the current durable context rather than relying on a stale local cache.</summary>
    public string? Read()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM AgentState WHERE Key = 'context';";
        return command.ExecuteScalar() as string;
    }

    /// <summary>Replaces the explicit context using parameterized SQL.</summary>
    public void Set(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 4096)
            throw new ArgumentException("Context must not exceed 4096 characters.");
        Execute("INSERT INTO AgentState(Key, Value) VALUES ('context', $value) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value;", value);
    }

    /// <summary>Clears rows transactionally without deleting pooled SQLite files.</summary>
    public void Reset() => Execute("DELETE FROM AgentState;");

    private void Execute(string sql, string? value = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        if (value is not null)
            command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
        transaction.Commit();
    }
}

/// <summary>Persists MAF sessions with an instruction version, atomic replacement, and explicit failures.</summary>
public sealed class SessionFile(string path, string instructionVersion)
{
    /// <summary>Restores a session; incompatible instructions or corrupted state fail visibly.</summary>
    public async Task<AgentSession> LoadOrCreateAsync(AIAgent agent, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            return await agent.CreateSessionAsync(cancellationToken);
        var envelope = JsonSerializer.Deserialize<Envelope>(await File.ReadAllTextAsync(path, cancellationToken))
            ?? throw new InvalidDataException("Empty session envelope.");
        if (envelope.Version != 1 || envelope.InstructionVersion != instructionVersion)
            throw new InvalidDataException("Session version or instruction version mismatch. Reset explicitly.");
        return await agent.DeserializeSessionAsync(envelope.Session, cancellationToken: cancellationToken);
    }

    /// <summary>Saves to a temporary neighbor before replacing the last successful snapshot.</summary>
    public async Task SaveAsync(AIAgent agent, AgentSession session, CancellationToken cancellationToken = default)
    {
        var envelope = new Envelope(1, instructionVersion, await agent.SerializeSessionAsync(session, cancellationToken: cancellationToken));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(envelope), cancellationToken);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    /// <summary>Deletes the durable snapshot. The caller also replaces every active session.</summary>
    public void Delete() => File.Delete(path);

    private sealed record Envelope(int Version, string InstructionVersion, JsonElement Session);
}
