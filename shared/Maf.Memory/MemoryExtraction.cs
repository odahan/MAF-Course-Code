using System.Text.Json;
using Microsoft.Agents.AI;

namespace Maf.Memory;

/// <summary>A structured extraction; validation and replacement belong to the host.</summary>
public sealed class FactExtraction
{
    /// <summary>Up to five candidates quoting the current user's words.</summary>
    public List<FactCandidate> Facts { get; set; } = [];
}

/// <summary>Runs a stateless writer. Failure is observable and never replaced with invented fallback data.</summary>
public sealed class MemoryExtraction(AIAgent writer, WorkingMemory memory)
{
    /// <summary>Extracts from the current user only, forwards cancellation and reports format/provider failures.</summary>
    public async Task<string> UpdateAsync(string userText, CancellationToken cancellationToken = default)
    {
        var turn = memory.BeginTurn(userText);
        try
        {
            var response = await writer.RunAsync<FactExtraction>(
                JsonSerializer.Serialize(new { userText }),
                serializerOptions: JsonSerializerOptions.Web, cancellationToken: cancellationToken);
            var declarations = ExplicitAssertions.Parse(userText);
            if (response.Result.Facts is null || response.Result.Facts.Count != declarations.Count
                || response.Result.Facts.Any(f => f is null || !declarations.TryGetValue(f.Key, out var value) || value != f.Value))
                throw new InvalidDataException("Extraction does not match the explicit key=value assertions. No semantic guess is admitted.");
            memory.Apply(turn, response.Result.Facts, DateTimeOffset.UtcNow);
            return $"Memory updated: {response.Result.Facts.Count} candidate(s), {memory.Snapshot().Count} retained fact(s). "
                + $"Extraction provider-reported tokens: input={response.Usage?.InputTokenCount?.ToString() ?? "unknown"}, "
                + $"output={response.Usage?.OutputTokenCount?.ToString() ?? "unknown"}.";
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            return $"Memory update failed ({exception.GetType().Name}); previous facts retained. No fallback was applied.";
        }
    }
}

/// <summary>Defines the narrow assertion syntax admitted as durable memory; free prose remains conversation data.</summary>
public static class ExplicitAssertions
{
    /// <summary>Reads semicolon-separated assignments; the final occurrence of a key is the correction.</summary>
    public static IReadOnlyDictionary<string, string> Parse(string text)
    {
        var assertions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string segment in text.Split([';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = segment.IndexOf('=');
            if (separator < 1)
                continue;
            string key = segment[..separator].Trim();
            string value = segment[(separator + 1)..].Trim();
            if (!WorkingMemory.AllowedKeys.Contains(key) || string.IsNullOrWhiteSpace(value) || value.Length > 256)
                throw new ArgumentException("Explicit assertions require an allowed key and a value of 1 to 256 characters.");
            assertions[key] = value;
        }
        if (assertions.Count > 5)
            throw new ArgumentException("At most five explicit assertions are admitted per message.");
        return assertions;
    }
}

/// <summary>Shares user assertions across agents without feeding generated answers back into the writer.</summary>
public sealed class SharedMemoryProvider(WorkingMemory memory, MemoryExtraction extraction, Action<string> report)
    : AIContextProvider(null, null)
{
    /// <inheritdoc />
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new AIContext
        {
            Messages = [new(Microsoft.Extensions.AI.ChatRole.User, memory.ToPromptBlock())]
        });

    /// <inheritdoc />
    protected override async ValueTask StoreAIContextAsync(InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.InvokeException is not null)
            return;
        foreach (var message in context.RequestMessages.Where(m => m.Role == Microsoft.Extensions.AI.ChatRole.User
            && m.GetAgentRequestMessageSourceType() == AgentRequestMessageSourceType.External))
        {
            if (!string.IsNullOrWhiteSpace(message.Text))
                report(await extraction.UpdateAsync(message.Text, cancellationToken));
        }
    }
}
