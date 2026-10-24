using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;
using OpenAI;

namespace Maf.DemoConnection;

/// <summary>Creates model clients without adding agent instructions or business policies.</summary>
public static class DemoConnection
{
    /// <summary>Creates an owned chat client. Null follows configuration; true selects Ollama; false selects the API.</summary>
    /// <param name="options">Independent connection profiles.</param>
    /// <param name="isLocal">Optional explicit override for chat only.</param>
    /// <param name="handler">Optional transport handler, whose ownership transfers to the returned client.</param>
    public static IChatClient CreateChatClient(ConnectionOptions? options = null, bool? isLocal = null,
        HttpMessageHandler? handler = null)
    {
        options ??= new();
        var provider = isLocal.HasValue
            ? (isLocal.Value ? ConnectionProvider.Ollama : ConnectionProvider.OpenAICompatible)
            : options.ChatProvider;
        ValidateProvider(provider);
        var profile = provider == ConnectionProvider.Ollama ? options.LocalChat : options.ApiChat;
        var endpoint = ValidateProfile(profile, options);
        var key = GetApiKey(provider, profile);
        var http = CreateTransport(endpoint, options, handler);
        try
        {
            IChatClient client = provider == ConnectionProvider.Ollama
                ? new OllamaApiClient(http, profile.Model)
                : new OpenAIClient(new ApiKeyCredential(key!), new OpenAIClientOptions
                {
                    Endpoint = endpoint,
                    Transport = new HttpClientPipelineTransport(http),
                    RetryPolicy = new ClientRetryPolicy(0),
                    NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
                }).GetChatClient(profile.Model).AsIChatClient();
            return new OwnedChatClient(client, http);
        }
        catch
        {
            http.Dispose();
            throw;
        }
    }

    /// <summary>Creates an owned embedding generator with no dependency on the chat profile.</summary>
    public static IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(
        ConnectionOptions? options = null, HttpMessageHandler? handler = null)
    {
        options ??= new();
        ValidateProvider(options.EmbeddingProvider);
        var endpoint = ValidateProfile(options.Embeddings, options);
        if (options.EmbeddingDimensions is <= 0)
            throw new ArgumentException("AI:EmbeddingDimensions must be positive when specified.");
        var key = GetApiKey(options.EmbeddingProvider, options.Embeddings);
        return new HttpEmbeddingGenerator(CreateTransport(endpoint, options, handler), options, key);
    }

    /// <summary>Registers singleton clients that are disposed by the console or backend host.</summary>
    public static IServiceCollection AddDemoConnection(this IServiceCollection services, ConnectionOptions options)
    {
        services.AddSingleton<IChatClient>(_ => CreateChatClient(options));
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(_ => CreateEmbeddingGenerator(options));
        return services;
    }

    internal static void ValidateProvider(ConnectionProvider provider)
    {
        if (!Enum.IsDefined(provider))
            throw new ArgumentException("The AI provider must be Ollama or OpenAICompatible.");
    }

    internal static Uri ValidateProfile(ModelProfile profile, ConnectionOptions options)
    {
        if (options.TimeoutSeconds is < 1 or > 3600)
            throw new ArgumentException("AI:TimeoutSeconds must be between 1 and 3600.");
        if (string.IsNullOrWhiteSpace(profile.Model))
            throw new ArgumentException("The selected AI profile requires a model.");
        if (!Uri.TryCreate(profile.Endpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != "http" && endpoint.Scheme != "https")
            || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new ArgumentException("The selected AI endpoint must be an absolute HTTP(S) URL without credentials, query or fragment.");
        return new Uri(endpoint.AbsoluteUri.TrimEnd('/') + "/");
    }

    internal static string? GetApiKey(ConnectionProvider provider, ModelProfile profile)
    {
        if (provider == ConnectionProvider.Ollama)
            return null;
        if (string.IsNullOrWhiteSpace(profile.ApiKeyEnvironmentVariable))
            throw new ArgumentException("The selected API profile requires a secret environment variable name.");
        var key = DemoConfiguration.ReadSecret(profile.ApiKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(key) || key.Any(char.IsControl))
            throw new ArgumentException("The selected API profile requires a nonempty API key in its configured environment variable.");
        return key;
    }

    private static HttpClient CreateTransport(Uri endpoint, ConnectionOptions options, HttpMessageHandler? handler)
        => new(handler ?? new HttpClientHandler { AllowAutoRedirect = false }, disposeHandler: true)
        {
            BaseAddress = endpoint,
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };

    private sealed class OwnedChatClient(IChatClient inner, HttpClient http) : DelegatingChatClient(inner)
    {
        protected override void Dispose(bool disposing)
        {
            try { base.Dispose(disposing); }
            finally { if (disposing) http.Dispose(); }
        }
    }
}
