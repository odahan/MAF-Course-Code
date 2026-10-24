using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Maf.DemoConnection;

/// <summary>Describes an editable non-secret configuration field.</summary>
public sealed record ConfigurationField(string Key, string Group, string Description, string Kind = "text");

/// <summary>Preserves unrelated sections and rejects invalid or conflicting configuration writes.</summary>
public sealed class DemoConfigurationFile
{
    private readonly JsonObject document;
    private byte[] originalHash;

    /// <summary>Gets the file edited by this session.</summary>
    public string FilePath { get; }

    /// <summary>Gets editable fields; API key values are intentionally excluded.</summary>
    public static IReadOnlyList<ConfigurationField> Fields { get; } =
    [
        new("AI:ChatProvider", "Chat", "Active chat provider", "provider"),
        new("AI:LocalChat:Endpoint", "Ollama", "Ollama server URL", "url"),
        new("AI:LocalChat:Model", "Ollama", "Local chat model"),
        new("AI:ApiChat:Endpoint", "Cloud", "OpenAI-compatible API base URL, including /v1/", "url"),
        new("AI:ApiChat:Model", "Cloud", "Cloud chat model"),
        new("AI:ApiChat:ApiKeyEnvironmentVariable", "Cloud", "Environment variable containing the cloud key", "variable"),
        new("AI:EmbeddingProvider", "Embeddings", "Independent embedding provider", "provider"),
        new("AI:Embeddings:Endpoint", "Embeddings", "Embedding server URL", "url"),
        new("AI:Embeddings:Model", "Embeddings", "Embedding model; changing it requires rebuilding vector indexes"),
        new("AI:Embeddings:ApiKeyEnvironmentVariable", "Embeddings", "Environment variable containing the embedding API key", "variable"),
        new("AI:EmbeddingDimensions", "Embeddings", "Optional positive dimension; blank uses the model default", "dimension"),
        new("AI:TimeoutSeconds", "Advanced", "Per-request timeout, 1–3600 seconds", "timeout"),
        new("MAF_STATE_ROOT", "Storage", "Optional absolute root for demo state", "path"),
        new("MAF_DEMO_DB_PATH", "Storage", "Optional absolute backlog database path", "path"),
        new("MAF_DEMO_READ_ONLY", "Storage", "Backlog host policy: 1 denies writes; 0 permits guarded writes", "switch"),
        new("Rag:Corpus", "RAG", "Optional absolute corpus JSON path", "path"),
        new("Rag:Database", "RAG", "Optional absolute vector SQLite database path", "path"),
        new("Rag:AsOf", "RAG", "Optional eligibility date, yyyy-MM-dd", "date"),
        new("MAF_BACKEND_URL", "Interfaces", "Backend URL used by console and WPF", "url"),
        new("Backend:Url", "Interfaces", "Backend URL used by the Web client", "url"),
        new("Conversation:StateRoot", "Interfaces", "Optional absolute streaming-session directory", "path"),
        new("Mcp:ServerPath", "Interfaces", "Optional absolute precompiled MCP server DLL", "path"),
        new("MAF_INSTRUCTION_PROFILE", "Advanced", "Optional instruction profile identifier"),
        new("MAF_ROUTE_TRIAGE", "Routing", "Optional installed Ollama model for triage"),
        new("MAF_ROUTE_EASY", "Routing", "Optional installed Ollama model for easy requests"),
        new("MAF_ROUTE_HARD", "Routing", "Optional installed Ollama model for hard requests")
    ];

    /// <summary>Loads an existing file without applying environment overrides or changing it.</summary>
    public DemoConfigurationFile(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
        byte[] bytes = File.ReadAllBytes(FilePath);
        document = JsonNode.Parse(bytes) as JsonObject ?? throw new ArgumentException("Configuration must be a JSON object.");
        originalHash = SHA256.HashData(bytes);
    }

    /// <summary>Gets a stored scalar value without reading secret environment values.</summary>
    public string GetValue(string key)
    {
        JsonNode? node = document;
        foreach (var segment in key.Split(':')) node = node?[segment];
        return node?.ToString() ?? "";
    }

    /// <summary>Changes an allowlisted field in memory; blank optional values remove the override.</summary>
    public void SetValue(string key, string value)
    {
        var field = Fields.SingleOrDefault(field => field.Key == key)
            ?? throw new ArgumentException("Unknown setting. API key values cannot be written to configuration.");
        ValidateField(field, value);
        var segments = key.Split(':');
        var parent = document;
        foreach (var segment in segments[..^1])
        {
            if (parent[segment] is null) parent[segment] = new JsonObject();
            parent = parent[segment] as JsonObject ?? throw new ArgumentException("Configuration section must be an object.");
        }
        parent[segments[^1]] = string.IsNullOrWhiteSpace(value) ? null
            : field.Kind is "timeout" or "dimension" ? JsonValue.Create(int.Parse(value, CultureInfo.InvariantCulture))
            : JsonValue.Create(value.Trim());
    }

    /// <summary>Validates stored profiles without network access, installed models or API keys.</summary>
    public void Validate()
    {
        foreach (var field in Fields) ValidateField(field, GetValue(field.Key));
        using var configuration = (ConfigurationRoot)new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(document))).Build();
        var settings = ConnectionOptions.FromConfiguration(configuration);
        DemoConnection.ValidateProvider(settings.ChatProvider);
        DemoConnection.ValidateProvider(settings.EmbeddingProvider);
        DemoConnection.ValidateProfile(settings.LocalChat, settings);
        var chat = settings.ChatProvider == ConnectionProvider.Ollama ? settings.LocalChat : settings.ApiChat;
        DemoConnection.ValidateProfile(chat, settings);
        DemoConnection.ValidateProfile(settings.Embeddings, settings);
    }

    /// <summary>Saves atomically with a unique backup; external edits are never silently overwritten.</summary>
    public string Save()
    {
        Validate();
        using var lease = new FileStream(FilePath + ".edit.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        byte[] previous = File.ReadAllBytes(FilePath);
        if (!SHA256.HashData(previous).SequenceEqual(originalHash))
            throw new IOException("Configuration changed outside this session. Reload it before saving.");
        var backup = FilePath + ".backup-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture)
            + "-" + Guid.NewGuid().ToString("N");
        File.Copy(FilePath, backup, overwrite: false);
        var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
            File.Move(temporary, FilePath, overwrite: true);
            originalHash = SHA256.HashData(File.ReadAllBytes(FilePath));
            return backup;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void ValidateField(ConfigurationField field, string value)
    {
        if (value.Any(char.IsControl)) throw new ArgumentException($"{field.Key}: control characters are not allowed.");
        if (string.IsNullOrWhiteSpace(value))
        {
            if (field.Kind is "provider" or "timeout" || field.Key.EndsWith(":Endpoint", StringComparison.Ordinal))
                throw new ArgumentException($"{field.Key}: a value is required.");
            return;
        }
        bool valid = field.Kind switch
        {
            "provider" => value is "Ollama" or "OpenAICompatible",
            "timeout" => int.TryParse(value, out var seconds) && seconds is >= 1 and <= 3600,
            "dimension" => int.TryParse(value, out var dimension) && dimension > 0,
            "switch" => value is "0" or "1",
            "path" => Path.IsPathFullyQualified(value),
            "variable" => char.IsAsciiLetter(value[0]) && value.All(character => char.IsAsciiLetterOrDigit(character) || character == '_'),
            "date" => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "url" => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
                && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0,
            _ => true
        };
        if (!valid) throw new ArgumentException($"{field.Key}: invalid {field.Kind} value.");
    }
}
