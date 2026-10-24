using Maf.DemoConnection;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddDemoConfiguration(args);
var settings = ConnectionOptions.FromConfiguration(builder.Configuration);
builder.Services.AddDemoConnection(settings);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ready", chat = settings.ChatProvider.ToString(), embeddings = settings.EmbeddingProvider.ToString() }));
app.MapPost("/chat", async (PromptRequest request, IServiceProvider services, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
        return Results.BadRequest(new { error = "Text is required." });
    try
    {
        var chat = services.GetRequiredService<IChatClient>();
        var response = await chat.GetResponseAsync(request.Text, cancellationToken: cancellationToken);
        return Results.Ok(new { text = response.Text });
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
    catch (ArgumentException) { return Results.Problem("Invalid AI configuration.", statusCode: 503); }
    catch (Exception) { return Results.Problem("The selected model service is unavailable.", statusCode: 502); }
});
app.Run();

/// <summary>Contains a user message for the connection example.</summary>
public sealed record PromptRequest(string Text);
