using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Maf.Observability;

/// <summary>Reports each actual model request separately, including tool loops; input/output text is never reported.</summary>
public sealed class ObservedChatClient(IChatClient inner, Action<Consumption> report) : DelegatingChatClient(inner)
{
    /// <summary>Captures provider usage at the request boundary and sanitizes failure status.</summary>
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = Telemetry.Source.StartActivity("model");
        try
        {
            var response = await base.GetResponseAsync(messages, options, cancellationToken);
            report(Consumption.Measure(response.Usage));
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch
        {
            report(Consumption.Measure(null));
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }

    /// <summary>Aggregates standard usage deltas once at stream completion; interrupted streams remain unknown.</summary>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = Telemetry.Source.StartActivity("model");
        UsageDetails? usage = null;
        bool completed = false;
        try
        {
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                foreach (var content in update.Contents.OfType<UsageContent>())
                {
                    usage ??= new();
                    usage.Add(content.Details);
                }
                yield return update;
            }
            completed = true;
        }
        finally
        {
            report(Consumption.Measure(completed ? usage : null));
            activity?.SetStatus(completed ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        }
    }
}
