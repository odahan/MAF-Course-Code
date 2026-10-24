using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Maf.Rag;

/// <summary>Describes a governed source; validity includes both boundary dates.</summary>
public sealed record SourceDocument(string Id, string Scope, string Title, string Text, string Status,
    bool Canonical, DateOnly ValidFrom, DateOnly? ValidUntil, string Product = "GENERAL",
    string? Procedure = null);

/// <summary>Describes a separately maintained, authoritative product fact.</summary>
public sealed record ProductFact(string Product, string Status, string Procedure, string Treatment);

/// <summary>Contains the reproducible corpus and structured business data.</summary>
public sealed record Corpus(SourceDocument[] Documents, ProductFact[] Products)
{
    /// <summary>Loads and validates a corpus before making any index changes.</summary>
    public static Corpus Load(string path)
    {
        var corpus = JsonSerializer.Deserialize<Corpus>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("Missing corpus.");
        corpus.Validate();
        return corpus;
    }

    /// <summary>Rejects ambiguous identities, invalid intervals and oversized demo inputs.</summary>
    public void Validate()
    {
        if (Documents is null || Products is null || Documents.Length == 0 || Documents.Length > 500
            || Documents.Any(d => d is null || !Regex.IsMatch(d.Id ?? "", @"^[A-Za-z0-9_-]+$")
                || string.IsNullOrWhiteSpace(d.Scope) || string.IsNullOrWhiteSpace(d.Title)
                || string.IsNullOrWhiteSpace(d.Text) || d.Text.Length > 10000
                || d.ValidUntil < d.ValidFrom)
            || Documents.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != Documents.Length
            || Products.Any(p => p is null || string.IsNullOrWhiteSpace(p.Product)
                || string.IsNullOrWhiteSpace(p.Procedure) || string.IsNullOrWhiteSpace(p.Treatment))
            || Products.Select(p => p.Product).Distinct(StringComparer.Ordinal).Count() != Products.Length)
            throw new InvalidDataException("Invalid or duplicate corpus records.");
    }

    /// <summary>Gets the stable JSON representation used by files and fingerprints.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web) { WriteIndented = true };
}

/// <summary>Identifies an exact excerpt independently of filesystem enumeration order.</summary>
public sealed record Chunk(string Id, SourceDocument Source, string Text);

/// <summary>Records embedding identity and every parameter that affects this demo index.</summary>
public sealed record IndexManifest(string Provider, string Endpoint, string Model, int Dimensions,
    string Distance, string Chunker, string CorpusHash)
{
    /// <summary>Uses ordinal source ordering so reordered files have the same fingerprint.</summary>
    public static IndexManifest Create(Corpus corpus, string provider, string endpoint, string model, int dimensions)
        => new(provider, endpoint.TrimEnd('/'), model, dimensions, "Cosine", "chars-600-overlap-80-v1",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new Corpus(corpus.Documents.OrderBy(d => d.Id, StringComparer.Ordinal).ToArray(),
                    corpus.Products.OrderBy(p => p.Product, StringComparer.Ordinal).ToArray()), Corpus.JsonOptions)))));
}

/// <summary>Applies host-owned eligibility before either retrieval branch or top-K.</summary>
public static class CorpusPolicy
{
    /// <summary>Evaluates the supplied date rather than the machine clock.</summary>
    public static bool Eligible(SourceDocument source, DateOnly asOf, string scope, string? product = null)
        => source.Scope == scope && source.Status == "active" && source.Canonical
            && source.ValidFrom <= asOf && (source.ValidUntil is null || source.ValidUntil >= asOf)
            && (product is null || source.Product == product || source.Product == "GENERAL");

    /// <summary>Splits normalized source text using fixed, versioned character boundaries.</summary>
    public static Chunk[] Chunks(Corpus corpus)
    {
        corpus.Validate();
        var result = new List<Chunk>();
        foreach (var source in corpus.Documents.OrderBy(d => d.Id, StringComparer.Ordinal))
        {
            string text = source.Text.Replace("\r\n", "\n");
            for (int start = 0, part = 0; start < text.Length; start += 520, part++)
            {
                result.Add(new($"{source.Id}#{part}", source, text.Substring(start, Math.Min(600, text.Length - start))));
                if (start + 600 >= text.Length) break;
            }
        }
        return result.ToArray();
    }
}

/// <summary>Retains raw cosine separately from the hybrid rank.</summary>
public sealed record SearchHit(Chunk Chunk, float Cosine, double Rank);

/// <summary>Contains only excerpts actually retrieved for the current question.</summary>
public sealed record Retrieval(string Question, DateOnly AsOf, SearchHit[] Hits, ProductFact? Product);

/// <summary>Allows the model to select an exact quote, never to authorize its own evidence.</summary>
public sealed record Evidence(string SourceId, string Quote);

/// <summary>Is the structured response proposed by the model.</summary>
public sealed record AnswerProposal(bool CanAnswer, Evidence[] Evidence);

/// <summary>Contains the final application-controlled result.</summary>
public sealed record ControlledAnswer(bool Accepted, string Text, Evidence[] Evidence)
{
    /// <summary>Creates a refusal without retaining generated prose or citations.</summary>
    public static ControlledAnswer Refuse(string reason) => new(false, "Insufficient evidence: " + reason, []);
}

/// <summary>Checks citations and renders only exact quotations from retrieved excerpts.</summary>
public static class AnswerGate
{
    /// <summary>Rejects invented citations, altered quotations and invalid model contracts.</summary>
    public static ControlledAnswer Validate(AnswerProposal? proposal, Retrieval retrieval)
    {
        if (retrieval.Hits.Length == 0 || proposal is not { CanAnswer: true, Evidence.Length: > 0 and <= 3 })
            return ControlledAnswer.Refuse("no supported answer.");
        var sources = retrieval.Hits.ToDictionary(h => h.Chunk.Id, StringComparer.Ordinal);
        foreach (var evidence in proposal.Evidence)
        {
            if (evidence is null || string.IsNullOrWhiteSpace(evidence.SourceId)
                || !sources.TryGetValue(evidence.SourceId, out var source)
                || !CorpusPolicy.Eligible(source.Chunk.Source, retrieval.AsOf, source.Chunk.Source.Scope)
                || string.IsNullOrWhiteSpace(evidence.Quote) || evidence.Quote.Trim().Length < 20
                || !source.Chunk.Text.Contains(evidence.Quote, StringComparison.Ordinal))
                return ControlledAnswer.Refuse("invalid citation or quotation.");
        }
        return new(true, string.Join("\n", proposal.Evidence.Select(e => $"[{e.SourceId}] {e.Quote}")), proposal.Evidence);
    }
}
