using Maf.DemoConnection;
using Microsoft.Extensions.AI;

namespace Maf.Rag;

/// <summary>Connects the common embedding generator to the persistent SQLite vector index.</summary>
public sealed class EmbeddingIndex(ConnectionOptions settings, Corpus corpus, SqliteVectorStore store) : IDisposable
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> embeddings =
        DemoConnection.DemoConnection.CreateEmbeddingGenerator(settings);

    /// <summary>Generates bounded batches with consistent finite, nonzero vectors.</summary>
    public async Task<float[][]> EmbedAsync(IEnumerable<string> texts, CancellationToken ct = default)
    {
        var result = new List<float[]>();
        foreach (var batch in texts.Chunk(16))
        {
            var generated = await embeddings.GenerateAsync(batch, cancellationToken: ct);
            result.AddRange(generated.Select(v => v.Vector.ToArray()));
        }
        if (result.Count == 0 || result[0].Length == 0 || result.Any(v => v.Length != result[0].Length
            || v.Any(x => !float.IsFinite(x)) || !v.Any(x => x != 0)))
            throw new InvalidDataException("Invalid embedding vectors.");
        return result.ToArray();
    }

    /// <summary>Completes embedding before replacing SQLite data in a transaction.</summary>
    public async Task<IndexManifest> RebuildAsync(CancellationToken ct = default)
    {
        var vectors = await EmbedAsync(CorpusPolicy.Chunks(corpus).Select(c => c.Text), ct);
        var manifest = Manifest(vectors[0].Length);
        store.Rebuild(corpus, manifest, vectors, ct);
        return manifest;
    }

    /// <summary>Retrieves actual cosine scores or a controlled hybrid context.</summary>
    public async Task<Retrieval> RetrieveAsync(string question, DateOnly asOf, string scope,
        bool diagnostics = false, CancellationToken ct = default, bool rawCorpus = false)
    {
        if (rawCorpus && !diagnostics) throw new ArgumentException("Raw corpus access is diagnostic only.");
        if (string.IsNullOrWhiteSpace(question) || question.Length > 4096)
            throw new ArgumentException("Question must contain 1 to 4096 characters.");
        var vector = (await EmbedAsync([question], ct))[0];
        var manifest = Manifest(vector.Length);
        var productId = scope == "business" ? RetrievalPolicy.DetectProduct(question) : null;
        var product = productId is null ? null : store.FindProduct(corpus, manifest, productId);
        var hits = store.Search(corpus, manifest, vector, asOf, scope, productId, ct, applyGovernance: !rawCorpus);
        if (!diagnostics)
        {
            if (scope == "business")
                hits = product is { Status: "active" }
                    ? hits.Where(h => h.Chunk.Source.Procedure == product.Procedure
                        && h.Chunk.Source.Product == product.Product).ToArray() : [];
            hits = RetrievalPolicy.Rank(question, hits);
        }
        return new(question, asOf, hits, product);
    }

    private IndexManifest Manifest(int dimensions) => IndexManifest.Create(corpus, settings.EmbeddingProvider.ToString(),
        settings.Embeddings.Endpoint, settings.Embeddings.Model, dimensions);

    /// <summary>Disposes the owned embedding transport.</summary>
    public void Dispose() => embeddings.Dispose();
}
