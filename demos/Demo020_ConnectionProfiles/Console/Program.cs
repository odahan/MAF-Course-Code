using Maf.DemoConnection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();
builder.Configuration.AddDemoConfiguration(args);
var settings = ConnectionOptions.FromConfiguration(builder.Configuration);
builder.Services.AddDemoConnection(settings);
using var host = builder.Build();
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
try
{
    if (args.Contains("--embed"))
    {
        var generator = host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var result = await generator.GenerateAsync(["A support ticket needs a clear description."], cancellationToken: cancellation.Token);
        Console.WriteLine($"Embeddings: {settings.EmbeddingProvider}, {settings.Embeddings.Model}, dimension {result[0].Vector.Length}.");
    }
    else
    {
        var chat = host.Services.GetRequiredService<IChatClient>();
        var model = settings.ChatProvider == ConnectionProvider.Ollama ? settings.LocalChat.Model : settings.ApiChat.Model;
        Console.WriteLine($"Chat: {settings.ChatProvider}, {model}.");
        var response = await chat.GetResponseAsync("Reply with one short greeting in English.", cancellationToken: cancellation.Token);
        Console.WriteLine(response.Text);
    }
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Request cancelled or timed out.");
    Environment.ExitCode = 2;
}
catch (ArgumentException error)
{
    Console.Error.WriteLine(error.Message);
    Environment.ExitCode = 1;
}
catch (Exception)
{
    Console.Error.WriteLine("The model request failed. Check the selected service and model.");
    Environment.ExitCode = 1;
}
