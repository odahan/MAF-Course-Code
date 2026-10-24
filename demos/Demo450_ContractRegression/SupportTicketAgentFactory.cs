using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentCiCd.Core;

/// <summary>Builds a MAF agent from a caller-owned common connection or deterministic test client.</summary>
public static class SupportTicketAgentFactory
{
    /// <summary>Creates a classifier with no external action; the caller owns the client.</summary>
    public static SupportTicketAgent Create(IChatClient client) => new(client.AsAIAgent(
        name: "TicketTriageAgent", instructions: "Classify support tickets using the requested typed contract. Never execute external actions."));
}
