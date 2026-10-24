using AgentCiCd.Core;
using Maf.Backlog;
using Maf.DemoConnection;

using var client = DemoConnection.CreateChatClient(DemoSettings.Load(args));
using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(2));
Console.Write("Support request > ");
var input = Console.ReadLine();
if (string.IsNullOrWhiteSpace(input)) return;
var result = await SupportTicketAgentFactory.Create(client).HandleAsync(input, stop.Token);
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));
Console.WriteLine("Classification only; no ticket was created. Tests exercise rules, not model quality.");
