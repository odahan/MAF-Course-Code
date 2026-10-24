using Microsoft.Extensions.AI;

namespace Maf.Control;

/// <summary>Drives real native tool loops with deterministic provider responses, without claiming model quality.</summary>
public sealed class ScriptedReviewClient(string scenario = "complete") : IChatClient
{
    /// <summary>Gets actual transport invocations.</summary>
    public int Calls { get; private set; }

    /// <summary>Returns scripted function requests to the native MAF function invocation loop.</summary>
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        if (scenario == "deadline") await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        if (scenario == "unavailable") throw new HttpRequestException("Scripted transport unavailable.");
        if (scenario == "incomplete" || (scenario == "complete" && Calls > 2))
            return new(new ChatMessage(ChatRole.Assistant, "Review finished."));
        var name = scenario is "forbidden" or "repeat" or "changing" || Calls == 1 ? "read_ticket" : "finish_review";
        var input = scenario == "changing" ? Calls : 42;
        return new(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent("call-" + Calls, name, new Dictionary<string, object?> { ["input"] = input })]));
    }

    /// <summary>Streaming is not implemented by this deterministic fixture.</summary>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    /// <summary>Returns transport metadata without ambient provider features.</summary>
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType == typeof(ChatClientMetadata) ? new ChatClientMetadata("scripted") : null;
    /// <summary>Has no unmanaged resources.</summary>
    public void Dispose() { }
}
