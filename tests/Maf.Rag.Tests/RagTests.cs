using Maf.Rag;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.Rag.Tests;

/// <summary>Exercises real SQLite persistence and the host-owned RAG invariants without a model or network.</summary>
[TestClass]
public sealed class RagTests
{
    private static readonly DateOnly Today = new(2026, 10, 23);

    private static SourceDocument Document(string id = "SOURCE", string text = "The source requires a named receipt for every train reimbursement.")
        => new(id, "corpus", "Train reimbursement", text, "active", true, new(2026, 1, 1), null);

    private static Corpus Data(params SourceDocument[] documents) => new(documents, []);
    private static IndexManifest Manifest(Corpus corpus) => IndexManifest.Create(corpus, "Ollama", "http://localhost:11434/", "embeddinggemma:300m", 2);
    private static string Database() => Path.Combine(Path.GetTempPath(), "maf-rag-tests", Guid.NewGuid().ToString("N"), "vectors.db");

    [TestMethod]
    public void ShippedCorpusExcludesDraftExpiredFutureAndMirror()
    {
        var corpus = Corpus.Load(Path.Combine(AppContext.BaseDirectory, "corpus.json"));
        CollectionAssert.AreEqual(new[] { "TRAIN-CURRENT" }, corpus.Documents
            .Where(d => CorpusPolicy.Eligible(d, Today, "corpus")).Select(d => d.Id).ToArray());
    }

    [TestMethod]
    public void BothDateBoundariesAreInclusive()
    {
        var source = Document() with { ValidFrom = Today, ValidUntil = Today };
        Assert.IsTrue(CorpusPolicy.Eligible(source, Today, "corpus"));
        Assert.IsFalse(CorpusPolicy.Eligible(source, Today.AddDays(-1), "corpus"));
        Assert.IsFalse(CorpusPolicy.Eligible(source, Today.AddDays(1), "corpus"));
    }

    [TestMethod]
    public void ReopeningSqlitePreservesActualDenseVectors()
    {
        string path = Database();
        var corpus = Data(Document());
        new SqliteVectorStore(path).Rebuild(corpus, Manifest(corpus), [[1, 0]]);
        var hit = new SqliteVectorStore(path).Search(corpus, Manifest(corpus), [1, 0], Today, "corpus").Single();
        Assert.AreEqual(1f, hit.Cosine, 0.00001);
        Assert.AreEqual("SOURCE#0", hit.Chunk.Id);
    }

    [TestMethod]
    public void RemovedDocumentsAndShortenedChunksLeaveNoResidue()
    {
        var corpus = Data(Document("LONG", new string('x', 1400)), Document("REMOVED"));
        var store = new SqliteVectorStore(Database());
        store.Rebuild(corpus, Manifest(corpus), CorpusPolicy.Chunks(corpus).Select(_ => new float[] { 1, 0 }).ToArray());
        var reduced = Data(Document("LONG", "Only the current short source should remain in the vector store."));
        store.Rebuild(reduced, Manifest(reduced), [[1, 0]]);
        var hits = store.Search(reduced, Manifest(reduced), [1, 0], Today, "corpus");
        Assert.HasCount(1, hits);
        Assert.AreEqual("LONG#0", hits[0].Chunk.Id);
    }

    [TestMethod]
    public void FiltersRunBeforeAnyTopKDespiteManyInvalidNearestVectors()
    {
        var docs = Enumerable.Range(0, 30).Select(i => Document($"FUTURE{i}") with { ValidFrom = Today.AddDays(1) })
            .Append(Document("ADMISSIBLE")).ToArray();
        var corpus = Data(docs);
        var store = new SqliteVectorStore(Database());
        store.Rebuild(corpus, Manifest(corpus), docs.Select(_ => new float[] { 1, 0 }).ToArray());
        var hits = store.Search(corpus, Manifest(corpus), [1, 0], Today, "corpus");
        Assert.HasCount(1, hits);
        Assert.AreEqual("ADMISSIBLE", hits[0].Chunk.Source.Id);
    }

    [TestMethod]
    public void ChangedModelEndpointDimensionMetricAndChunkerRequireRebuild()
    {
        var corpus = Data(Document());
        var manifest = Manifest(corpus);
        var store = new SqliteVectorStore(Database());
        store.Rebuild(corpus, manifest, [[1, 0]]);
        foreach (var changed in new[] { manifest with { Model = "different" }, manifest with { Endpoint = "http://other" },
            manifest with { Distance = "Dot" }, manifest with { Chunker = "other" }, manifest with { Provider = "OpenAICompatible" } })
            Assert.Throws<InvalidDataException>(() => store.Search(corpus, changed, [1, 0], Today, "corpus"));
        Assert.Throws<InvalidDataException>(() => store.Search(corpus, manifest with { Dimensions = 3 }, [1, 0, 0], Today, "corpus"));
    }

    [TestMethod]
    public void ChangedCorpusAndStructuredFactsRequireRebuild()
    {
        var corpus = Data(Document());
        var store = new SqliteVectorStore(Database());
        store.Rebuild(corpus, Manifest(corpus), [[1, 0]]);
        var changed = Data(Document() with { Text = "A revised rule requires different evidence." });
        Assert.Throws<InvalidDataException>(() => store.Search(changed, Manifest(changed), [1, 0], Today, "corpus"));
        changed = corpus with { Products = [new("GX-900", "active", "CQ-HT-42", "HT-42")] };
        Assert.Throws<InvalidDataException>(() => store.FindProduct(changed, Manifest(changed), "GX-900"));
    }

    [TestMethod]
    public void CanceledRebuildRollsBackInsteadOfDestroyingOldIndex()
    {
        var corpus = Data(Document());
        var store = new SqliteVectorStore(Database());
        store.Rebuild(corpus, Manifest(corpus), [[1, 0]]);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.Throws<OperationCanceledException>(() => store.Rebuild(Data(Document("NEW")), Manifest(corpus), [[0, 1]], canceled.Token));
        Assert.HasCount(1, store.Search(corpus, Manifest(corpus), [1, 0], Today, "corpus"));
    }

    [TestMethod]
    public void InvalidVectorsNeverReplaceExistingIndex()
    {
        var corpus = Data(Document());
        var store = new SqliteVectorStore(Database());
        store.Rebuild(corpus, Manifest(corpus), [[1, 0]]);
        foreach (float[] vector in new float[][] { [0, 0], [float.NaN, 1], [float.PositiveInfinity, 0], [1] })
            Assert.Throws<InvalidDataException>(() => store.Rebuild(corpus, Manifest(corpus), [vector]));
        Assert.HasCount(1, store.Search(corpus, Manifest(corpus), [1, 0], Today, "corpus"));
    }

    [TestMethod]
    public void CosineIsRealAndRejectsIncompatibleVectors()
    {
        Assert.AreEqual(0f, SqliteVectorStore.Cosine([1, 0], [0, 1]), 0.00001);
        Assert.AreEqual(-1f, SqliteVectorStore.Cosine([1, 0], [-2, 0]), 0.00001);
        Assert.Throws<InvalidDataException>(() => SqliteVectorStore.Cosine([1, 0], [1]));
    }

    [TestMethod]
    public void FingerprintDoesNotDependOnFileOrderAndChunkingStopsAtEnd()
    {
        var a = Document("A", new string('a', 601));
        var b = Document("B");
        Assert.AreEqual(Manifest(Data(a, b)), Manifest(Data(b, a)));
        Assert.HasCount(2, CorpusPolicy.Chunks(Data(a)));
        Assert.AreEqual(81, CorpusPolicy.Chunks(Data(a))[1].Text.Length);
    }

    [TestMethod]
    public void DuplicateSourcesAndInvalidIntervalsAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => Data(Document(), Document()).Validate());
        Assert.Throws<InvalidDataException>(() => Data(Document() with { ValidUntil = new(2020, 1, 1) }).Validate());
    }

    [TestMethod]
    public void LexicalOverlapCannotBypassCosineThresholdAndIrrelevanceCannotGainCanonicalBonus()
    {
        var chunk = CorpusPolicy.Chunks(Data(Document())).Single();
        Assert.IsEmpty(RetrievalPolicy.Rank("train receipt", [new(chunk, 0.2f, 0)]));
        Assert.IsEmpty(RetrievalPolicy.Rank("apple pie recipe", [new(chunk, 0.99f, 0)]));
        Assert.HasCount(1, RetrievalPolicy.Rank("train receipt", [new(chunk, 0.9f, 0)]));
    }

    [TestMethod]
    public void FabricatedCitationsAndAlteredQuotesAreRejected()
    {
        var retrieval = Context();
        Assert.IsFalse(AnswerGate.Validate(new(true, [new("INVENTED#0", retrieval.Hits[0].Chunk.Text)]), retrieval).Accepted);
        Assert.IsFalse(AnswerGate.Validate(new(true, [new("SOURCE#0", "No receipt is needed for a train reimbursement.")]), retrieval).Accepted);
        Assert.IsFalse(AnswerGate.Validate(new(true, [new("SOURCE#0", "receipt")]), retrieval).Accepted);
    }

    [TestMethod]
    public void ValidEvidenceRendersOnlyTheExactRetrievedQuotation()
    {
        var retrieval = Context();
        var answer = AnswerGate.Validate(new(true, [new("SOURCE#0", retrieval.Hits[0].Chunk.Text)]), retrieval);
        Assert.IsTrue(answer.Accepted);
        Assert.AreEqual("[SOURCE#0] " + retrieval.Hits[0].Chunk.Text, answer.Text);
    }

    [TestMethod]
    public void RawDiagnosticDocumentsCannotAuthorizeFutureOrNoncanonicalEvidence()
    {
        foreach (var source in new[] { Document() with { ValidFrom = Today.AddDays(1) },
            Document() with { Canonical = false }, Document() with { Status = "draft" } })
        {
            var chunk = CorpusPolicy.Chunks(Data(source)).Single();
            var retrieval = new Retrieval("train receipt", Today, [new(chunk, 1, 1)], null);
            Assert.IsFalse(AnswerGate.Validate(new(true, [new(chunk.Id, chunk.Text)]), retrieval).Accepted);
        }
    }

    [TestMethod]
    public void RawComparisonUsesRealVectorsButKeepsGovernanceOptionalOnlyForDiagnostics()
    {
        var corpus = Data(Document("CURRENT"), Document("FUTURE") with { ValidFrom = Today.AddDays(1) });
        var store = new SqliteVectorStore(Database());
        store.Rebuild(corpus, Manifest(corpus), [[0, 1], [1, 0]]);
        Assert.AreEqual("FUTURE", store.Search(corpus, Manifest(corpus), [1, 0], Today, "corpus", applyGovernance: false)[0].Chunk.Source.Id);
        Assert.AreEqual("CURRENT", store.Search(corpus, Manifest(corpus), [1, 0], Today, "corpus")[0].Chunk.Source.Id);
    }

    [TestMethod]
    public void MissingEvidenceRefusesBeforeCreatingAnyModelClient()
    {
        var answer = RagAnswerService.AnswerAsync(new() { ApiChat = new() { Model = "" } },
            new("question", Today, [], null), "tool").GetAwaiter().GetResult();
        Assert.IsFalse(answer.Accepted);
        Assert.IsEmpty(answer.Evidence);
    }

    [TestMethod]
    public void StructuredFactsPersistAndDisappearOnRebuild()
    {
        var corpus = Data(Document()) with { Products = [new("GX-900", "active", "CQ-HT-42", "HT-42")] };
        var store = new SqliteVectorStore(Database());
        store.Rebuild(corpus, Manifest(corpus), [[1, 0]]);
        Assert.AreEqual("CQ-HT-42", store.FindProduct(corpus, Manifest(corpus), "GX-900")?.Procedure);
        var reduced = corpus with { Products = [] };
        store.Rebuild(reduced, Manifest(reduced), [[1, 0]]);
        Assert.IsNull(store.FindProduct(reduced, Manifest(reduced), "GX-900"));
    }

    [TestMethod]
    public void TamperedPayloadAndTruncatedVectorAreRejected()
    {
        string path = Database();
        var corpus = Data(Document());
        var store = new SqliteVectorStore(path);
        store.Rebuild(corpus, Manifest(corpus), [[1, 0]]);
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE RagChunks SET Text='tampered'";
        command.ExecuteNonQuery();
        Assert.Throws<InvalidDataException>(() => store.Search(corpus, Manifest(corpus), [1, 0], Today, "corpus"));
        store.Rebuild(corpus, Manifest(corpus), [[1, 0]]);
        command.CommandText = "UPDATE RagChunks SET Vector=x'00000000'";
        command.ExecuteNonQuery();
        Assert.Throws<InvalidDataException>(() => store.Search(corpus, Manifest(corpus), [1, 0], Today, "corpus"));
    }

    private static Retrieval Context() => new("train reimbursement", Today,
        [new(CorpusPolicy.Chunks(Data(Document())).Single(), 0.9f, 1)], null);
}
