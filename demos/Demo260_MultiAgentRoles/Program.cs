using Microsoft.Extensions.AI;
using MafDay2.Common;

Console.WriteLine("Lab04 — Multi-agents (router)");
Console.WriteLine();

using IChatClient baseClient = TrainingConfig.CreateChatClient(args);
IChatClient chatClient = new ChatClientBuilder(baseClient).Build();

var backlogAgent = chatClient.AsAIAgent(
    name: "BacklogAgent",
    instructions: @"Tu tries une demande produit (Bug|Feature|Question) et proposes title + priority (low|medium|high). Réponse courte."
);

var knowledgeAgent = chatClient.AsAIAgent(
    name: "KnowledgeAgent",
    instructions: @"Tu réponds à une question technique interne. Si pas sûr: 'Je ne sais pas'."
);

var backlogSession = await backlogAgent.CreateSessionAsync();
var knowledgeSession = await knowledgeAgent.CreateSessionAsync();

while (true)
{
    Console.Write("Message (vide=quit) > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    var target = Route(input);
    if (target == RouteTarget.Knowledge)
    {
        var resp = await knowledgeAgent.RunAsync(input, knowledgeSession);
        Console.WriteLine("[RoutedTo=KnowledgeAgent]");
        Console.WriteLine(resp.Text);
    }
    else
    {
        var resp = await backlogAgent.RunAsync(input, backlogSession);
        Console.WriteLine("[RoutedTo=BacklogAgent]");
        Console.WriteLine(resp.Text);
    }
    Console.WriteLine();
}

static RouteTarget Route(string input)
{
    var s = input.Trim().ToLowerInvariant();
    if (s.StartsWith("où") || s.StartsWith("comment") || s.StartsWith("pourquoi"))
        return RouteTarget.Knowledge;
    if (s.Contains("401") || s.Contains("500") || s.Contains("logs") || s.Contains("rétention"))
        return RouteTarget.Knowledge;
    return RouteTarget.Backlog;
}

enum RouteTarget { Backlog, Knowledge }
