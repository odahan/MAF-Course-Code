using System.Net.Http.Json;
using System.Text.Json;

namespace Streaming.Client;

/// <summary>Identifies a conversation and one turn; display history is never model state.</summary>
public sealed record DiscussionRequest(string SessionId, string Message);
/// <summary>Frames text, completion, or errors independently of HTTP read boundaries.</summary>
public sealed record StreamEvent(string Type, string? Text = null);

/// <summary>Consumes the same backend from console and desktop clients.</summary>
public sealed class DiscussionClient(HttpClient http)
{
    /// <summary>Preserves every text fragment and rejects errors or a truncated stream.</summary>
    public async Task StreamAsync(DiscussionRequest request, Action<string> onText, CancellationToken token = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/discussion/flux")
        {
            Content = JsonContent.Create(request)
        };
        using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(token) is { } line)
        {
            var frame = JsonSerializer.Deserialize<StreamEvent>(line, JsonSerializerOptions.Web)
                ?? throw new InvalidDataException("Invalid stream event.");
            switch (frame.Type)
            {
                case "text": onText(frame.Text ?? throw new InvalidDataException("Missing text.")); break;
                case "completed": return;
                case "error": throw new InvalidOperationException(frame.Text ?? "Generation failed.");
                default: throw new InvalidDataException("Unknown stream event.");
            }
        }
        throw new EndOfStreamException("The response ended before completion; completion is unconfirmed.");
    }
}

