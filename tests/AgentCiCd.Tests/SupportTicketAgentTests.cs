using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AgentCiCd.Tests;

[TestClass]
public sealed class SupportTicketAgentTests
{
    [TestMethod]
    public async Task HandleAsync_CreeUnTicket_QuandLeRisqueEstFaible()
    {
        // Le faux modèle renvoie une décision stable et conforme au contrat.
        var agent = TestAgentFactory.Create("""
        {
          "action": "create_ticket",
          "category": "support",
          "riskScore": 10,
          "reason": "Demande simple de support utilisateur."
        }
        """, out var fakeClient);

        var result = await agent.HandleAsync("Je n'arrive pas à ouvrir mon espace support.");

        Assert.AreEqual("create_ticket", result.FinalAction);
        Assert.AreEqual("support", result.Category);
        Assert.AreEqual(10, result.RiskScore);
        Assert.IsTrue(
            fakeClient.LastPromptText.Contains("contract", StringComparison.OrdinalIgnoreCase),
            "Le prompt doit rappeler explicitement le contrat attendu.");
    }

    [TestMethod]
    public async Task HandleAsync_RouteVersValidationHumaine_QuandLeRisqueEstEleve()
    {
        // Même si le modèle propose de créer un ticket, la policy applicative reprend le contrôle.
        var agent = TestAgentFactory.Create("""
        {
          "action": "create_ticket",
          "category": "billing",
          "riskScore": 85,
          "reason": "Demande financière sensible."
        }
        """, out _);

        var result = await agent.HandleAsync("Rembourse immédiatement cette facture litigieuse.");

        Assert.AreEqual("human_review", result.FinalAction);
        Assert.AreEqual("billing", result.Category);
    }

    [TestMethod]
    public async Task HandleAsync_RefuseUneActionInterdite()
    {
        // Cette sortie simule un modèle qui propose une action hors contrat.
        var agent = TestAgentFactory.Create("""
        {
          "action": "refund_client",
          "category": "billing",
          "riskScore": 20,
          "reason": "Le client réclame un remboursement."
        }
        """, out _);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            agent.HandleAsync("Rembourse le client."));

        StringAssert.Contains(exception.Message, "Forbidden action");
    }

    [TestMethod]
    public async Task HandleAsync_RefuseUnScoreDeRisqueInvalide()
    {
        // Le contrat métier impose une borne numérique vérifiable.
        var agent = TestAgentFactory.Create("""
        {
          "action": "create_ticket",
          "category": "support",
          "riskScore": 150,
          "reason": "Score volontairement invalide."
        }
        """, out _);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            agent.HandleAsync("Teste cette demande."));

        StringAssert.Contains(exception.Message, "Invalid risk score");
    }

    [TestMethod]
    public async Task HandleAsync_RefuseUneJustificationVide()
    {
        // Une réponse typée peut être techniquement structurée mais rester inacceptable métier.
        var agent = TestAgentFactory.Create("""
        {
          "action": "create_ticket",
          "category": "support",
          "riskScore": 5,
          "reason": ""
        }
        """, out _);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            agent.HandleAsync("Classe cette demande."));

        StringAssert.Contains(exception.Message, "justification");
    }
}
