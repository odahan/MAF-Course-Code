using System.Text.Json;
using Maf.DemoConnection;
using Maf.Memory;
using Microsoft.Extensions.AI;

var settings = MemoryDemo.Load(args);
using var chat = DemoConnection.CreateChatClient(settings);
var profiles = JsonSerializer.Deserialize<InstructionProfile[]>(
    await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "appsettings.json")),
    JsonSerializerOptions.Web) ?? throw new InvalidDataException("Missing instruction profiles.");
if (profiles.Length == 0 || profiles.Any(p => string.IsNullOrWhiteSpace(p.Id)
    || string.IsNullOrWhiteSpace(p.Version) || string.IsNullOrWhiteSpace(p.Instructions))
    || profiles.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != profiles.Length)
    throw new InvalidDataException("Instruction profiles must have unique IDs, versions, and instructions.");
string selected = global::Maf.DemoConnection.DemoConfiguration.GetSetting("MAF_INSTRUCTION_PROFILE") ?? "support-cautious";
var profile = profiles.SingleOrDefault(p => p.Id == selected)
    ?? throw new InvalidDataException("Unknown instruction profile.");
Console.WriteLine($"Instructions: {profile.Id}/{profile.Version}. Effective connection provider: {settings.ChatProvider}.");
Console.WriteLine("The instruction profile does not select a model; AI settings select the connection.");
using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(5));
var agent = chat.AsAIAgent(name: profile.Id, instructions: profile.Instructions);
var response = await agent.RunAsync("Billing is blocked for three customers. No precise error is available. Suggest the next support action.", cancellationToken: stop.Token);
Console.WriteLine(response.Text);
MemoryDemo.PrintUsage(response);

/// <summary>A versioned business instruction profile, independent of provider/model selection.</summary>
public sealed record InstructionProfile(string Id, string Version, string Instructions);

