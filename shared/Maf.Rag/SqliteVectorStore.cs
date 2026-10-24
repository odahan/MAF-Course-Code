using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Maf.Rag;

/// <summary>Stores dense vectors in SQLite and computes exact cosine in C# without a service or extension.</summary>
public sealed class SqliteVectorStore
{
    private readonly string connectionString;

    /// <summary>Uses an absolute caller-owned database path.</summary>
    public SqliteVectorStore(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("An absolute SQLite path is required.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
    }

    /// <summary>Replaces only this module's tables atomically, including obsolete chunks and product facts.</summary>
    public void Rebuild(Corpus corpus, IndexManifest manifest, IReadOnlyList<float[]> vectors, CancellationToken ct = default)
    {
        var chunks = CorpusPolicy.Chunks(corpus);
        if (vectors.Count != chunks.Length) throw new InvalidDataException("Vector count differs from chunk count.");
        foreach (var vector in vectors) ValidateVector(vector, manifest.Dimensions);
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using (var schema = connection.CreateCommand())
        {
            schema.Transaction = transaction;
            schema.CommandText = """
                CREATE TABLE IF NOT EXISTS RagManifest (Id INTEGER PRIMARY KEY CHECK(Id=1), Json TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS RagChunks (
                    Id TEXT PRIMARY KEY, SourceJson TEXT NOT NULL, Text TEXT NOT NULL, Vector BLOB NOT NULL,
                    Scope TEXT NOT NULL, Status TEXT NOT NULL, Canonical INTEGER NOT NULL,
                    ValidFrom INTEGER NOT NULL, ValidUntil INTEGER, Product TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS RagProducts (Product TEXT PRIMARY KEY, Json TEXT NOT NULL);
                DELETE FROM RagManifest;
                DELETE FROM RagChunks;
                DELETE FROM RagProducts;
                """;
            schema.ExecuteNonQuery();
        }
        for (int i = 0; i < chunks.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            var chunk = chunks[i];
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO RagChunks VALUES ($id,$source,$text,$vector,$scope,$status,$canonical,$from,$until,$product)";
            command.Parameters.AddWithValue("$id", chunk.Id);
            command.Parameters.AddWithValue("$source", JsonSerializer.Serialize(chunk.Source, Corpus.JsonOptions));
            command.Parameters.AddWithValue("$text", chunk.Text);
            var bytes = new byte[vectors[i].Length * 4];
            for (int j = 0; j < vectors[i].Length; j++)
                System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(j * 4, 4), vectors[i][j]);
            command.Parameters.AddWithValue("$vector", bytes);
            command.Parameters.AddWithValue("$scope", chunk.Source.Scope);
            command.Parameters.AddWithValue("$status", chunk.Source.Status);
            command.Parameters.AddWithValue("$canonical", chunk.Source.Canonical ? 1 : 0);
            command.Parameters.AddWithValue("$from", chunk.Source.ValidFrom.DayNumber);
            command.Parameters.AddWithValue("$until", (object?)chunk.Source.ValidUntil?.DayNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("$product", chunk.Source.Product);
            command.ExecuteNonQuery();
        }
        foreach (var product in corpus.Products)
        {
            ct.ThrowIfCancellationRequested();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO RagProducts VALUES ($id,$json)";
            command.Parameters.AddWithValue("$id", product.Product);
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(product, Corpus.JsonOptions));
            command.ExecuteNonQuery();
        }
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO RagManifest VALUES (1,$json)";
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(manifest, Corpus.JsonOptions));
            command.ExecuteNonQuery();
        }
        ct.ThrowIfCancellationRequested();
        transaction.Commit();
    }

    /// <summary>Filters in SQL before scoring every eligible vector; never truncates candidates before filters.</summary>
    public SearchHit[] Search(Corpus corpus, IndexManifest expected, float[] query, DateOnly asOf,
        string scope, string? product = null, CancellationToken ct = default, bool applyGovernance = true)
    {
        ValidateVector(query, expected.Dimensions);
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        ValidateManifest(connection, transaction, expected, corpus);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT Id, SourceJson, Text, Vector FROM RagChunks
            WHERE Scope=$scope AND ($governance=0 OR (Status='active' AND Canonical=1
                AND ValidFrom <= $date AND (ValidUntil IS NULL OR ValidUntil >= $date)
                AND ($product IS NULL OR Product=$product OR Product='GENERAL')))
            ORDER BY Id COLLATE BINARY
            """;
        command.Parameters.AddWithValue("$scope", scope);
        command.Parameters.AddWithValue("$date", asOf.DayNumber);
        command.Parameters.AddWithValue("$product", (object?)product ?? DBNull.Value);
        command.Parameters.AddWithValue("$governance", applyGovernance ? 1 : 0);
        var known = CorpusPolicy.Chunks(corpus).ToDictionary(c => c.Id, StringComparer.Ordinal);
        var hits = new List<SearchHit>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ct.ThrowIfCancellationRequested();
            var source = JsonSerializer.Deserialize<SourceDocument>(reader.GetString(1), Corpus.JsonOptions);
            if (!known.TryGetValue(reader.GetString(0), out var chunk) || chunk.Source != source
                || chunk.Text != reader.GetString(2)
                || applyGovernance && !CorpusPolicy.Eligible(chunk.Source, asOf, scope, product))
                throw new InvalidDataException("Index payload differs from the corpus; rebuild required.");
            var bytes = (byte[])reader[3];
            if (bytes.Length != expected.Dimensions * 4) throw new InvalidDataException("Invalid stored vector length.");
            var vector = new float[expected.Dimensions];
            for (int i = 0; i < vector.Length; i++)
                vector[i] = System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * 4, 4));
            hits.Add(new(chunk, Cosine(query, vector), 0));
        }
        return hits.OrderByDescending(h => h.Cosine).ThenBy(h => h.Chunk.Id, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Reads a structured fact only after validating the stored corpus fingerprint.</summary>
    public ProductFact? FindProduct(Corpus corpus, IndexManifest expected, string product)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        ValidateManifest(connection, transaction, expected, corpus);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Json FROM RagProducts WHERE Product=$product";
        command.Parameters.AddWithValue("$product", product);
        var json = command.ExecuteScalar() as string;
        var fact = json is null ? null : JsonSerializer.Deserialize<ProductFact>(json, Corpus.JsonOptions);
        if (fact != corpus.Products.SingleOrDefault(p => p.Product == product))
            throw new InvalidDataException("Structured data differs; rebuild required.");
        return fact;
    }

    /// <summary>Uses double precision accumulators and rejects zero, nonfinite or incompatible vectors.</summary>
    public static float Cosine(float[] left, float[] right)
    {
        ValidateVector(left, left.Length);
        ValidateVector(right, left.Length);
        double dot = 0, a = 0, b = 0;
        for (int i = 0; i < left.Length; i++)
        {
            dot += (double)left[i] * right[i];
            a += (double)left[i] * left[i];
            b += (double)right[i] * right[i];
        }
        return (float)Math.Clamp(dot / Math.Sqrt(a * b), -1, 1);
    }

    private static void ValidateVector(float[] vector, int dimensions)
    {
        if (dimensions <= 0 || vector.Length != dimensions || vector.Any(x => !float.IsFinite(x)) || !vector.Any(x => x != 0))
            throw new InvalidDataException("Invalid vector dimensions, values or norm.");
    }

    private static void ValidateManifest(SqliteConnection connection, SqliteTransaction transaction,
        IndexManifest expected, Corpus corpus)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT Json FROM RagManifest WHERE Id=1";
        try
        {
            var json = command.ExecuteScalar() as string;
            if (json is null || JsonSerializer.Deserialize<IndexManifest>(json, Corpus.JsonOptions) != expected
                || expected.Distance != "Cosine")
                throw new InvalidDataException("Index invariants differ; rebuild required.");
            command.CommandText = "SELECT COUNT(*) FROM RagChunks";
            if ((long)command.ExecuteScalar()! != CorpusPolicy.Chunks(corpus).Length)
                throw new InvalidDataException("Incomplete index; rebuild required.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            throw new InvalidDataException("Index schema missing or incompatible; rebuild required.", ex);
        }
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }
}
