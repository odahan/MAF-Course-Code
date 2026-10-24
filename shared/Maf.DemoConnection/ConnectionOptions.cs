using Microsoft.Extensions.Configuration;

namespace Maf.DemoConnection;

/// <summary>Defines the two supported connection protocols.</summary>
public enum ConnectionProvider
{
    /// <summary>Uses the Ollama API.</summary>
    Ollama,
    /// <summary>Uses the OpenAI-compatible API with an API key.</summary>
    OpenAICompatible
}

/// <summary>Describes a model endpoint without storing its secret.</summary>
public sealed record ModelProfile
{
    /// <summary>Gets the service base URL, including /v1 for OpenAI-compatible services.</summary>
    public string Endpoint { get; init; } = "http://localhost:11434/";
    /// <summary>Gets the model identifier.</summary>
    public string Model { get; init; } = "granite4:3b";
    /// <summary>Gets the name of the environment variable containing the API key.</summary>
    public string ApiKeyEnvironmentVariable { get; init; } = "MAF_API_KEY";
}

/// <summary>Contains independent chat and embedding settings.</summary>
public sealed record ConnectionOptions
{
    /// <summary>Gets the configured chat provider. The default is local.</summary>
    public ConnectionProvider ChatProvider { get; init; } = ConnectionProvider.Ollama;
    /// <summary>Gets the local chat profile.</summary>
    public ModelProfile LocalChat { get; init; } = new();
    /// <summary>Gets the explicitly selected API chat profile.</summary>
    public ModelProfile ApiChat { get; init; } = new() { Endpoint = "https://api.openai.com/v1/", Model = "" };
    /// <summary>Gets the independent embedding provider.</summary>
    public ConnectionProvider EmbeddingProvider { get; init; } = ConnectionProvider.Ollama;
    /// <summary>Gets the embedding model profile, independent of chat selection.</summary>
    public ModelProfile Embeddings { get; init; } = new() { Model = "embeddinggemma:300m" };
    /// <summary>Gets the timeout applied to each transport request.</summary>
    public int TimeoutSeconds { get; init; } = 120;
    /// <summary>Gets an optional dimension enforced on every embedding response.</summary>
    public int? EmbeddingDimensions { get; init; }

    /// <summary>Loads the AI section from any console or host configuration.</summary>
    public static ConnectionOptions FromConfiguration(IConfiguration configuration)
    {
        try
        {
            return configuration.GetSection("AI").Get<ConnectionOptions>() ?? new();
        }
        catch (InvalidOperationException)
        {
            throw new ArgumentException("The AI configuration has an invalid type or provider value.");
        }
    }
}
