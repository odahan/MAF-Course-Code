using System.Text.Json;
using Maf.DemoConnection;
using Maf.AgentCapabilities;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddDemoConfiguration(args).Build();
var settings = ConnectionOptions.FromConfiguration(configuration);
var profile = settings.ChatProvider == ConnectionProvider.Ollama ? settings.LocalChat : settings.ApiChat;
using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var results = new List<CapabilityResult>();
try
{
    using var client = DemoConnection.CreateChatClient(settings);
    results.AddRange(await new CapabilityPilot(client,
        disableReasoning: configuration["Pilot:ReasoningEffort"] == "none").RunAsync(cancellation.Token));
}
catch (Exception error)
{
    results.Add(new("configuration", "Failed", error.GetType().Name, null));
}
var report = new
{
    checkedAt = DateTimeOffset.UtcNow,
    provider = settings.ChatProvider.ToString(),
    model = profile.Model,
    reasoningEffort = configuration["Pilot:ReasoningEffort"] ?? "provider default",
    protocol = settings.ChatProvider == ConnectionProvider.Ollama ? "Ollama /api/chat" : "OpenAI /v1/chat/completions",
    results
};
var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
if (configuration["Report"] is { Length: > 0 } path)
    await File.WriteAllTextAsync(path, json);
Environment.ExitCode = results.Count == 5 && results.All(r => r.Status == "Passed") ? 0 : 1;
