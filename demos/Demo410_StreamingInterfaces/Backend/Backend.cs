using System.Net;
using System.Text;
using System.Text.Json;
using Maf.DemoConnection;
using Microsoft.Extensions.AI;
using Streaming.Client;

namespace Streaming.Server;

/// <summary>Builds the local HTTP backend; tests can replace inference while retaining real HTTP.</summary>
public static class Backend
{
    /// <summary>Creates endpoints with bounded requests, exact CORS origins and explicit stream outcomes.</summary>
    public static WebApplication Create(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddDemoConfiguration(args);
        if (builder.Configuration["urls"] is null) builder.WebHost.UseUrls("http://localhost:5088");
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 32768);
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5089"])
            .WithMethods("POST").WithHeaders("Content-Type")));
        builder.Services.AddSingleton<IChatClient>(_ => DemoConnection.CreateChatClient(ConnectionOptions.FromConfiguration(builder.Configuration)));
        builder.Services.AddSingleton<ConversationEngine>();
        configure?.Invoke(builder);
        var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Connection.RemoteIpAddress is { } address && !IPAddress.IsLoopback(address))
            {
                context.Response.StatusCode = 403;
                return;
            }
            await next(context);
        });
        app.UseCors();
        app.MapGet("/", () => new { status = "Ready", conversation = "Durable successful turns; local single-process demo" });
        app.MapPost("/discussion", (DiscussionRequest request, HttpContext context, ConversationEngine engine) => RunAsync(request, context, engine, false));
        app.MapPost("/discussion/flux", (DiscussionRequest request, HttpContext context, ConversationEngine engine) => RunAsync(request, context, engine, true));
        return app;
    }

    private static async Task RunAsync(DiscussionRequest request, HttpContext context, ConversationEngine engine, bool streaming)
    {
        if (!Guid.TryParseExact(request.SessionId, "N", out _) || string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 4096)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Use a sessionId in GUID N format and a message of 1..4096 characters." });
            return;
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(120));
        var token = deadline.Token;
        var text = new StringBuilder();
        try
        {
            await engine.RunAsync(request, async fragment =>
            {
                if (streaming)
                {
                    if (!context.Response.HasStarted)
                    {
                        context.Response.ContentType = "application/x-ndjson; charset=utf-8";
                        context.Response.Headers.CacheControl = "no-store";
                    }
                    await WriteEventAsync(context, new("text", fragment), token);
                }
                else text.Append(fragment);
            }, token);
            if (streaming)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.ContentType = "application/x-ndjson; charset=utf-8";
                    context.Response.Headers.CacheControl = "no-store";
                }
                await WriteEventAsync(context, new("completed"), token);
            }
            else await context.Response.WriteAsJsonAsync(new { message = text.ToString() }, token);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (Exception error)
        {
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Discussion")
                .LogWarning("Conversation failed with {ErrorType}", error.GetType().Name);
            var publicError = error is OperationCanceledException ? "Generation deadline exceeded." : "Generation or delivery failed; completion was not confirmed.";
            if (context.Response.HasStarted && streaming)
                await WriteEventAsync(context, new("error", publicError), context.RequestAborted);
            else
            {
                context.Response.StatusCode = error is ConversationBusyException ? 409 : 502;
                await context.Response.WriteAsJsonAsync(new { error = publicError }, context.RequestAborted);
            }
        }
    }

    private static async Task WriteEventAsync(HttpContext context, StreamEvent frame, CancellationToken token)
    {
        await context.Response.WriteAsync(JsonSerializer.Serialize(frame, JsonSerializerOptions.Web) + "\n", token);
        await context.Response.Body.FlushAsync(token);
    }
}

