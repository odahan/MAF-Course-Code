using MafDay2.Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;


Console.WriteLine("Lab05 — Planning pattern (JSON)");
Console.WriteLine();

using IChatClient baseClient = TrainingConfig.CreateChatClient(args);
IChatClient chatClient = new ChatClientBuilder(baseClient).Build();

AIAgent planner = chatClient.AsAIAgent(
    name: "Planner",
    instructions: @"Tu produis un plan JSON court. Contrat:
{ ""steps"": [ { ""type"": ""retrieve|create_backlog|ask_user|respond"", ""argument"": ""..."" } ] }
Règles:
- pas de markdown
- 2 à 4 étapes max"
);

var session = await planner.CreateSessionAsync();
var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web);

while (true)
{
    Console.Write("Demande (vide=quit) > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) break;

    AgentResponse<Plan> planResp = await planner.RunAsync<Plan>(input, session: session, serializerOptions: jsonOpts);
    var plan = planResp.Result;

    Console.WriteLine("\n[PLAN]");
    int i = 1;
    foreach (var s in plan.Steps)
        Console.WriteLine($"{i++}. {s.Type} ({s.Argument})");

    Console.WriteLine("\n[EXECUTION SIMULÉE]");
    foreach (var step in plan.Steps)
    {
        if (!ExecuteStep(step))
        {
            Console.WriteLine("⛔ Arrêt: step inconnue.");
            break;
        }
    }
    Console.WriteLine();
}

static bool ExecuteStep(PlanStep step)
{
    var t = (step.Type ?? "").Trim().ToLowerInvariant();
    switch (t)
    {
        case "retrieve":
            Console.WriteLine($"- Retrieve context: {step.Argument}");
            return true;
        case "create_backlog":
            Console.WriteLine($"- Create backlog draft: {step.Argument} (approbation humaine en vraie implémentation)");
            return true;
        case "ask_user":
            Console.WriteLine($"- Ask user clarification: {step.Argument}");
            return true;
        case "respond":
            Console.WriteLine($"- Respond: {step.Argument}");
            return true;
        default:
            Console.WriteLine($"- Unknown type: {step.Type}");
            return false;
    }
}

public sealed class Plan { public List<PlanStep> Steps { get; set; } = new(); }
public sealed class PlanStep { public string Type { get; set; } = ""; public string? Argument { get; set; } }



