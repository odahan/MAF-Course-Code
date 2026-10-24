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

Console.WriteLine("Lab05 — Planning pattern (JSON) + JSON Schema validation");
Console.WriteLine();

using IChatClient baseClient = TrainingConfig.CreateChatClient(args);
IChatClient chatClient = new ChatClientBuilder(baseClient).Build();

AIAgent planner = chatClient.AsAIAgent(
    name: "Planner",
    instructions: """
    Tu produis un plan JSON court.

    Contrat (format attendu) :
    { \"steps\": [ { \"type\": \"retrieve|create_backlog|ask_user|respond\", \"argument\": \"...\" } ] }

    Règles :
    - renvoie UNIQUEMENT un JSON valide (aucun texte autour)
    - pas de markdown
    - 2 à 4 étapes max
    """
);

var session = await planner.CreateSessionAsync();
var jsonOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web);

while (true)
{
    Console.Write("Demande (vide=quit) > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
        break;

    // 1) Appel non typé : on veut le JSON BRUT (string)
    var rawResp = await planner.RunAsync(message: input, session: session);
    var rawText = GetResponseText(rawResp);

    // 2) Extraction/sanitarisation : on isole le JSON même si le modèle ajoute du texte/backticks
    var jsonText = ExtractJsonObject(rawText);

    // 3) Validation JSON Schema (+ retry unique si invalide)
    if (!ValidatePlanJson(jsonText, out var err))
    {
        Console.WriteLine("⚠️ Plan JSON invalide. Retry unique...");
        Console.WriteLine(err);

        var repairPrompt = $"""
Tu as renvoyé un JSON invalide ou non conforme.
Corrige-le et renvoie UNIQUEMENT un JSON valide, sans texte autour, conforme à ce schéma :

{PlanJsonSchema}

Demande utilisateur :
{input}
""";

        var rawResp2 = await planner.RunAsync(message: repairPrompt, session: session);
        var rawText2 = GetResponseText(rawResp2);
        jsonText = ExtractJsonObject(rawText2);

        if (!ValidatePlanJson(jsonText, out err))
        {
            Console.WriteLine("⛔ JSON toujours invalide après retry. Arrêt.");
            Console.WriteLine(err);
            Console.WriteLine();
            continue;
        }
    }

    // 4) Désérialisation une fois validé
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

    // Affichage plan
    Console.WriteLine("\n[PLAN]");
    int i = 1;
    foreach (var s in plan.Steps)
        Console.WriteLine($"{i++}. {s.Type} ({s.Argument})");

    // Exécution simulée (contrôlée)
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

// ---------- Helpers : réponse + extraction JSON ----------

static string GetResponseText(AgentResponse resp)
{
    // Selon provider, Text peut être null -> fallback ToString()
    if (!string.IsNullOrWhiteSpace(resp.Text))
        return resp.Text;

    return resp.ToString() ?? string.Empty;
}

static string ExtractJsonObject(string text)
{
    if (string.IsNullOrWhiteSpace(text))
        return string.Empty;

    text = text.Trim();

    // Gestion d’un éventuel bloc ```json ... ```
    if (text.StartsWith("```", StringComparison.Ordinal))
    {
        var firstNl = text.IndexOf('\n');
        if (firstNl >= 0)
            text = text[(firstNl + 1)..];

        var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
        if (lastFence >= 0)
            text = text[..lastFence];

        text = text.Trim();
    }

    // On récupère du premier '{' au dernier '}'
    int start = text.IndexOf('{');
    int end = text.LastIndexOf('}');
    if (start >= 0 && end > start)
        return text.Substring(start, end - start + 1).Trim();

    return text;
}

// ---------- Validation JSON Schema ----------

static bool ValidatePlanJson(string json, out string errorSummary)
{
    errorSummary = "";

    try
    {
        var schema = JsonSchema.FromText(PlanJsonSchema);

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

// ---------- Types ----------

public sealed class Plan
{
    public List<PlanStep> Steps { get; set; } = new();
}

public sealed class PlanStep
{
    public string Type { get; set; } = "";
    public string? Argument { get; set; }
}
