# Solution Exercice (Lab07) — Tokens + tags OpenTelemetry

Pattern attendu :

```csharp
using var act = activitySource.StartActivity("agent.run", ActivityKind.Internal);
var resp = await agent.RunAsync(q, session);

var usage = resp.Usage ?? ExtractUsageFallback(resp);
if (usage != null)
{
    var inTok = usage.InputTokenCount ?? 0;
    var outTok = usage.OutputTokenCount ?? 0;
    var total = usage.TotalTokenCount ?? (inTok + outTok);

    Console.WriteLine($"[TOKENS] in={inTok} out={outTok} total={total}");

    act?.SetTag("ai.tokens.input", inTok);
    act?.SetTag("ai.tokens.output", outTok);
    act?.SetTag("ai.tokens.total", total);
}
```

Fallback (si provider encode l’usage dans les messages) :
- chercher un `UsageContent` dans `resp.Messages[].Contents`.

Résultat : tokens + tags OTel.