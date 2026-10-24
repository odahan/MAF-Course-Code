/*
Code provided for private educational use only, without warranty.
See LICENCE.MD. Copyright Olivier Dahan 2026.
*/
using System.ComponentModel;
using System.Text.Json;
using Maf.Backlog;
using Maf.DemoConnection;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(2));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };

Console.WriteLine("Deterministic C#: a fixed discount rule returns " + (100m * 0.9m));
var request = "Login fails for a known account";
var component = request.Contains("Login", StringComparison.OrdinalIgnoreCase) ? "Auth" : "Other";
Console.WriteLine($"Explicit C# orchestration: classify -> route -> display. Component: {component}. This is not a MAF workflow graph.");
if (!args.Contains("--agent")) return;
var settings = DemoSettings.Load(args.Where(a => a != "--agent").ToArray());
using var chat = DemoConnection.CreateChatClient(settings);
Console.WriteLine($"Provider: {settings.ChatProvider}");

var agent = chat.AsAIAgent(name: "ArchitectureAgent", instructions:
    "Suggest two clarification questions for an ambiguous support request. Answer in English. No tools or actions are available.");
Console.WriteLine((await agent.RunAsync("The application sometimes fails. Help me investigate.", cancellationToken: stop.Token)).Text);
