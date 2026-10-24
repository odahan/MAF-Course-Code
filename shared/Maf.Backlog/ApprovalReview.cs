using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Maf.Backlog;

/// <summary>Tracks pending requests in one session and rejects changed, foreign or consumed requests.</summary>
public sealed class ApprovalReview(AgentSession session, WriteGate gate)
{
    private readonly Dictionary<ToolApprovalRequestContent, string> pending = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<string> seenCalls = [];

    /// <summary>Registers only observed requests; a call identifier cannot be registered twice.</summary>
    public void Register(AgentResponse response)
    {
        foreach (var request in response.Messages.SelectMany(m => m.Contents).OfType<ToolApprovalRequestContent>())
        {
            if (request.ToolCall is not FunctionCallContent call || !seenCalls.Add(call.CallId))
                throw new InvalidOperationException("Unsupported or repeated approval request.");
            pending.Add(request, Fingerprint(call));
        }
    }

    /// <summary>Consumes a matching request, binds the host grant and creates the native MAF response.</summary>
    public AIContent Decide(AgentSession currentSession, ToolApprovalRequestContent request, bool accepted)
    {
        if (!ReferenceEquals(session, currentSession) || !pending.Remove(request, out var original)
            || request.ToolCall is not FunctionCallContent call || original != Fingerprint(call))
            throw new InvalidOperationException("Approval is not pending in this session or its arguments changed.");
        string? id = null;
        if (call.Arguments is { Count: 1 } && call.Arguments.TryGetValue("proposalId", out var argument))
            id = argument is string text ? text : argument is JsonElement { ValueKind: JsonValueKind.String } element ? element.GetString() : null;
        var permitted = gate.Approve(call.Name, id, accepted);
        return request.CreateResponse(permitted);
    }

    private static string Fingerprint(FunctionCallContent call) => JsonSerializer.Serialize(new { call.CallId, call.Name, call.Arguments });
}
