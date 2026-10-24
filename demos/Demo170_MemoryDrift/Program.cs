using Maf.DemoConnection;
using Maf.Memory;

var settings = MemoryDemo.Load(args);
using var chat = DemoConnection.CreateChatClient(settings);
using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(10));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
Console.WriteLine($"Provider: {settings.ChatProvider}. EP013 chronological context comparison.");
await ContextExperiment.RunAsync(chat, stop.Token);

