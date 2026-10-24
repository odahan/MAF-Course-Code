# Solution Exercice (Lab07) — tool + HTTP instrumentation

TracerProvider :
```csharp
Sdk.CreateTracerProviderBuilder()
  .AddSource("MafWorkshop.Day2")
  .AddHttpClientInstrumentation()
  .AddConsoleExporter()
  .Build();
```

Tool span :
```csharp
private static readonly ActivitySource ToolSource = new("MafWorkshop.Day2");

public static Task<string> GetTimeAsync(string _ = "")
{
    using var act = ToolSource.StartActivity("tool.get_time", ActivityKind.Internal);
    var t = DateTimeOffset.Now.ToString("O");
    act?.SetTag("time.iso", t);
    return Task.FromResult(t);
}
```

Résultat :
- `agent.run` + `tool.get_time` + HTTP spans visibles dans la console.