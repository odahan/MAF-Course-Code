using AgentCiCd.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentCiCd.Tests;

/// <summary>
/// Fabrique de tests : elle construit un vrai agent MAF, mais branché sur un faux client.
/// C'est précisément ce qui rend le test représentatif sans appeler le vrai modèle.
/// </summary>
public static class TestAgentFactory
{
    public static SupportTicketAgent Create(string modelResponse, out DeterministicChatClient fakeClient)
    {
        fakeClient = new DeterministicChatClient(modelResponse);

        var mafAgent = new ChatClientAgent(
            fakeClient,
            new ChatClientAgentOptions
            {
                Name = "TicketTriageAgent_Test",
                Description = "Agent de test branché sur un faux client déterministe.",

                // Même en test, on conserve la forme MAF réelle de l'agent.
                // Seul le client de chat est remplacé par un double déterministe.
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                    Tu es un agent de tri de tickets de support.
                    Tu réponds en français et tu respectes le contrat typé demandé.
                    """
                }
            });

        return new SupportTicketAgent(mafAgent);
    }
}
