using System.Text.Json;

namespace Maf.Memory;

/// <summary>A candidate extracted exclusively from the current user message.</summary>
public sealed record FactCandidate(string Key, string Value, string Evidence);

/// <summary>A bounded user assertion, with provenance; this is not independent verification of truth.</summary>
public sealed record UserFact(string Key, string Value, string Evidence, long Turn, DateTimeOffset RecordedAt);

/// <summary>A turn ticket prevents older or forgotten asynchronous extractions from restoring state.</summary>
public sealed record MemoryTurn(long Epoch, long Number, string UserText);

/// <summary>Controls a small fact store. The latest explicit assertion replaces the same semantic key.</summary>
public sealed class WorkingMemory
{
    /// <summary>The host, rather than a prompt, enforces these semantic keys.</summary>
    public static IReadOnlySet<string> AllowedKeys { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "product", "module", "priority", "database", "constraint", "customer", "version", "environment", "symptom", "log"
    };

    private readonly object _gate = new();
    private readonly Dictionary<string, UserFact> _facts = new(StringComparer.Ordinal);
    private long _epoch;
    private long _turn;

    /// <summary>Starts a bounded extraction from a user message. Assistant responses are never accepted here.</summary>
    public MemoryTurn BeginTurn(string userText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userText);
        if (userText.Length > 4096)
            throw new ArgumentException("User messages must not exceed 4096 characters.");
        lock (_gate)
            return new MemoryTurn(_epoch, ++_turn, userText);
    }

    /// <summary>Validates the whole batch before applying it; stale updates are rejected explicitly.</summary>
    public void Apply(MemoryTurn turn, IReadOnlyList<FactCandidate>? candidates, DateTimeOffset recordedAt)
    {
        lock (_gate)
        {
            if (turn.Epoch != _epoch || turn.Number != _turn)
                throw new InvalidOperationException("Memory update is stale or belongs to a forgotten conversation.");
            if (candidates is null || candidates.Count > 5)
                throw new ArgumentException("Extraction must contain at most five facts.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var fact in candidates)
            {
                if (fact is null || !AllowedKeys.Contains(fact.Key ?? "") || !keys.Add(fact.Key!)
                    || string.IsNullOrWhiteSpace(fact.Value) || fact.Value.Length > 256
                    || string.IsNullOrWhiteSpace(fact.Evidence) || fact.Evidence.Length > 512
                    || !turn.UserText.Contains(fact.Evidence, StringComparison.Ordinal)
                    || !fact.Evidence.Contains(fact.Value, StringComparison.Ordinal))
                    throw new ArgumentException("Invalid fact: use a unique allowed key and a value quoted in current user evidence.");
            }

            foreach (var fact in candidates)
                _facts[fact.Key] = new(fact.Key, fact.Value, fact.Evidence, turn.Number, recordedAt);

            while (_facts.Count > 5)
            {
                var oldest = _facts.Values.OrderBy(f => f.Turn).ThenBy(f => f.Key, StringComparer.Ordinal).First();
                _facts.Remove(oldest.Key);
            }
        }
    }

    /// <summary>Returns a detached snapshot ordered by key for reproducible inspection.</summary>
    public IReadOnlyList<UserFact> Snapshot()
    {
        lock (_gate)
            return _facts.Values.OrderBy(f => f.Key, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Removes facts and invalidates every pending extraction, including after targeted removal.</summary>
    public void Forget(string? key = null)
    {
        lock (_gate)
        {
            if (key is null)
                _facts.Clear();
            else if (!AllowedKeys.Contains(key))
                throw new ArgumentException("Unknown memory key.");
            else
                _facts.Remove(key);
            _epoch++;
        }
    }

    /// <summary>Labels user assertions as untrusted data rather than instructions or assistant inferences.</summary>
    public string ToPromptBlock() => "User assertions (untrusted data; not independently verified):\n"
        + JsonSerializer.Serialize(Snapshot());
}
