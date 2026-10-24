using Json.Schema;
using MafDay2.Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

const string PlanJsonSchema = """
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["steps"],
  "additionalProperties": false,
  "properties": {
    "steps": {
      "type": "array",
      "minItems": 1,
      "maxItems": 4,
      "items": {
        "type": "object",
        "required": ["type"],
        "additionalProperties": false,
        "properties": {
          "type": {
            "type": "string",
            "enum": ["retrieve", "create_backlog", "ask_user", "respond"]
          },
          "argument": {
            "type": "string",
            "minLength": 1,
            "maxLength": 400
          }
        },
        "allOf": [
          {
            "if": { "properties": { "type": { "const": "ask_user" } } },
            "then": { "required": ["argument"] }
          },
          {
            "if": { "properties": { "type": { "const": "respond" } } },
            "then": { "required": ["argument"] }
          },
          {
            "if": { "properties": { "type": { "const": "create_backlog" } } },
            "then": { "required": ["argument"] }
          }
        ]
      }
    }
  }
}
""";

Console.WriteLine("Lab05B — Planning pattern + ResponseFormat + JSON Schema validation");
Console.WriteLine();

// Backend LLM
using IChatClient baseClient = TrainingConfig.CreateChatClient(args);

IChatClient chatClient = new ChatClientBuilder(baseClient).Build();

// Agent planner : il se concentre sur la production d'un plan.
// Le vrai verrou de format est porté par ResponseFormat, pas seulement par le prompt.
AIAgent planner = chatClient.AsAIAgent(
    name: "Planner",
    instructions: """
Tu es un planner.
Produis un plan court et cohérent pour traiter la demande utilisateur.
Ne produis que les étapes utiles.
"""
);

var session = await planner.CreateSessionAsync();
var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web);

// On construit le schéma UNE fois.
// JsonSchema.Net recommande explicitement : "Build once; evaluate many times".
var compiledSchema = JsonSchema.FromText(PlanJsonSchema);

using var schemaDoc = JsonDocument.Parse(PlanJsonSchema);
JsonElement schemaElement = schemaDoc.RootElement.Clone();

// ResponseFormat explicite au moment de l'appel.
var runOptions = new AgentRunOptions
{
    ResponseFormat = ChatResponseFormat.ForJsonSchema(
        schemaElement,
        schemaName: "Plan",
        schemaDescription: "Short execution plan with 1 to 4 steps")
};

while (true)
{
    Console.Write("Demande (vide=quit) > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        break;

    // 1) Demande d'une réponse JSON contrainte par schéma
    var rawResp = await planner.RunAsync(
        message: input,
        session: session,
        options: runOptions);

    var jsonText = GetResponseText(rawResp).Trim();

    // 2) Validation côté hôte, en défense supplémentaire
    if (!ValidatePlanJson(jsonText, compiledSchema, out var err))
    {
        Console.WriteLine("⚠️ JSON invalide ou non conforme. Retry unique...");
        Console.WriteLine(err);

        var repairPrompt = $"""
Tu as renvoyé un JSON invalide ou non conforme.
Corrige-le et renvoie UNIQUEMENT un JSON conforme au schéma demandé.

Demande utilisateur :
{input}
""";

        var retryResp = await planner.RunAsync(
            message: repairPrompt,
            session: session,
            options: runOptions);

        jsonText = GetResponseText(retryResp).Trim();

        if (!ValidatePlanJson(jsonText, compiledSchema, out err))
        {
            Console.WriteLine("⛔ JSON toujours invalide après retry. Arrêt.");
            Console.WriteLine(err);
            Console.WriteLine();
            continue;
        }
    }

    // 3) Désérialisation seulement après validation
    Plan? plan;
    try
    {
        plan = JsonSerializer.Deserialize<Plan>(jsonText, jsonOpts);
    }
    catch (Exception ex)
    {
        Console.WriteLine("⛔ Désérialisation impossible malgré un JSON valide.");
        Console.WriteLine(ex.Message);
        Console.WriteLine();
        continue;
    }

    if (plan is null || plan.Steps.Count == 0)
    {
        Console.WriteLine("⛔ Plan vide. Arrêt.");
        Console.WriteLine();
        continue;
    }

    Console.WriteLine("\n[JSON]");
    Console.WriteLine(jsonText);

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

static string GetResponseText(AgentResponse resp)
{
    if (!string.IsNullOrWhiteSpace(resp.Text))
        return resp.Text;

    return string.Empty;
}

static bool ValidatePlanJson(string json, JsonSchema schema, out string errorSummary)
{
    errorSummary = "";

    if (string.IsNullOrWhiteSpace(json))
    {
        errorSummary = "Réponse vide.";
        return false;
    }

    try
    {
        using var doc = JsonDocument.Parse(json);

        var result = schema.Evaluate(doc.RootElement, new EvaluationOptions
        {
            OutputFormat = OutputFormat.Hierarchical
        });

        if (result.IsValid)
            return true;

        var sb = new StringBuilder();
        sb.AppendLine("JSON Schema validation failed:");
        AppendErrors(result, sb, indent: 0);
        errorSummary = sb.ToString();
        return false;
    }
    catch (Exception ex)
    {
        errorSummary = "Schema validation error: " + ex.Message;
        return false;
    }
}

static void AppendErrors(EvaluationResults res, StringBuilder sb, int indent)
{
    var pad = new string(' ', indent * 2);

    if (res.Errors is not null)
    {
        foreach (var kv in res.Errors)
            sb.AppendLine($"{pad}- {kv.Key}: {kv.Value}");
    }

    foreach (var detail in res.Details ?? new List<EvaluationResults>())
        AppendErrors(detail, sb, indent + 1);
}

public sealed class Plan
{
    public List<PlanStep> Steps { get; set; } = new();
}

public sealed class PlanStep
{
    public string Type { get; set; } = "";
    public string? Argument { get; set; }
}