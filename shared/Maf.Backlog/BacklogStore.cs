using Microsoft.Data.Sqlite;

namespace Maf.Backlog;

/// <summary>Persists demo rows using parameterized commands; never resets a database at startup.</summary>
public sealed class BacklogStore(string path)
{
    private SqliteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS BacklogItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT, CreatedUtc TEXT NOT NULL,
                Title TEXT NOT NULL, Kind TEXT NOT NULL, Priority TEXT NOT NULL,
                Component TEXT NOT NULL, Summary TEXT NOT NULL, Status TEXT NOT NULL);
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    /// <summary>Revalidates the contract immediately before a real local write.</summary>
    public long Insert(BacklogDraft draft)
    {
        if (draft.Validate().Length != 0) throw new ArgumentException("The draft is not actionable.");
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO BacklogItems (CreatedUtc, Title, Kind, Priority, Component, Summary, Status)
            VALUES ($utc, $title, $kind, $priority, $component, $summary, 'New');
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$title", draft.Title);
        command.Parameters.AddWithValue("$kind", draft.Kind);
        command.Parameters.AddWithValue("$priority", draft.Priority);
        command.Parameters.AddWithValue("$component", draft.Component);
        command.Parameters.AddWithValue("$summary", draft.Summary);
        return (long)command.ExecuteScalar()!;
    }

    /// <summary>Returns stored rows as evidence, including an empty list when no data exists.</summary>
    public IReadOnlyList<BacklogItem> List()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Kind, Priority, Component, Summary FROM BacklogItems ORDER BY Id DESC LIMIT 20";
        using var reader = command.ExecuteReader();
        var items = new List<BacklogItem>();
        while (reader.Read()) items.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetString(4), reader.GetString(5)));
        return items;
    }
}
