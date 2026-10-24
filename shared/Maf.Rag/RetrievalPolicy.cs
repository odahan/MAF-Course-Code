using System.Text.RegularExpressions;

namespace Maf.Rag;

/// <summary>Fuses actual cosine retrieval and exact lexical overlap using reciprocal rank fusion.</summary>
public static class RetrievalPolicy
{
    /// <summary>Is the conservative cosine threshold qualified only on the delivered acceptance corpus.</summary>
    public const float MinCosine = 0.60f;

    /// <summary>Ranks all eligible candidates, then selects at most three supported excerpts.</summary>
    public static SearchHit[] Rank(string question, IEnumerable<SearchHit> candidates, float minCosine = MinCosine)
    {
        if (!float.IsFinite(minCosine) || minCosine < -1 || minCosine > 1)
            throw new ArgumentOutOfRangeException(nameof(minCosine));
        var all = candidates.OrderByDescending(h => h.Cosine).ThenBy(h => h.Chunk.Id, StringComparer.Ordinal).ToArray();
        var terms = Terms(question);
        var lexical = all.Select(h => (h.Chunk.Id, Overlap: Terms(h.Chunk.Source.Title + " " + h.Chunk.Text).Intersect(terms).Count()))
            .Where(h => h.Overlap > 0).OrderByDescending(h => h.Overlap).ThenBy(h => h.Id, StringComparer.Ordinal)
            .Select((h, i) => (h.Id, Rank: i + 1)).ToDictionary(h => h.Id, h => h.Rank);
        return all.Select((h, i) => h with { Rank = 1.0 / (60 + i + 1)
                + (lexical.TryGetValue(h.Chunk.Id, out int rank) ? 1.0 / (60 + rank) : 0) })
            .Where(h => h.Cosine >= minCosine && lexical.ContainsKey(h.Chunk.Id))
            .OrderByDescending(h => h.Rank).ThenBy(h => h.Chunk.Id, StringComparer.Ordinal).Take(3).ToArray();
    }

    /// <summary>Finds explicit product references without inventing missing product context.</summary>
    public static string? DetectProduct(string question)
    {
        var match = Regex.Match(question, @"\bGX-\d{3}\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static HashSet<string> Terms(string text) => Regex.Matches(text.ToLowerInvariant(), @"[\p{L}\p{N}-]+")
        .Select(m => m.Value).Where(t => t.Length > 2 && !StopWords.Contains(t)).ToHashSet(StringComparer.Ordinal);

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "the", "and", "for", "with", "what", "which", "how", "can", "does", "apply", "after", "without", "please",
        "les", "des", "une", "pour", "avec", "quelle", "quel", "comment", "peut", "sans", "est", "après"
    };
}
