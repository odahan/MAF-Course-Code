using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Maf.DemoConnection;

/// <summary>Uses batched embedding APIs with explicit rejection of silent truncation.</summary>
internal sealed class HttpEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly HttpClient http;
    private readonly ConnectionOptions settings;
    private readonly EmbeddingGeneratorMetadata metadata;

    public HttpEmbeddingGenerator(HttpClient http, ConnectionOptions settings, string? key)
    {
        this.http = http;
        this.settings = settings;
        metadata = new(settings.EmbeddingProvider.ToString(), http.BaseAddress,
            settings.Embeddings.Model, settings.EmbeddingDimensions);
        if (key is not null)
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (options?.ModelId is not null && options.ModelId != settings.Embeddings.Model
            || options?.Dimensions is not null && options.Dimensions != settings.EmbeddingDimensions
            || options?.AdditionalProperties is { Count: > 0 })
            throw new NotSupportedException("Configure the embedding model and dimensions in AI settings; additional embedding options are not supported.");
        var inputs = values.ToArray();
        if (inputs.Length == 0 || inputs.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Embedding input must contain at least one nonempty text.");
        var local = settings.EmbeddingProvider == ConnectionProvider.Ollama;
        object body = local
            ? new { model = settings.Embeddings.Model, input = inputs, truncate = false, dimensions = settings.EmbeddingDimensions }
            : new { model = settings.Embeddings.Model, input = inputs, encoding_format = "float", dimensions = settings.EmbeddingDimensions };
        using var response = await http.PostAsJsonAsync(local ? "api/embed" : "embeddings", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Embedding service returned HTTP {(int)response.StatusCode}.", null, response.StatusCode);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        float[][] vectors;
        try
        {
            vectors = local
                ? json.RootElement.GetProperty("embeddings").EnumerateArray().Select(ReadVector).ToArray()
                : ReadApiVectors(json.RootElement.GetProperty("data"), inputs.Length);
        }
        catch (Exception error) when (error is KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new InvalidOperationException("The embedding service returned an invalid response.");
        }
        var dimension = settings.EmbeddingDimensions ?? vectors.FirstOrDefault()?.Length ?? 0;
        if (vectors.Length != inputs.Length || dimension == 0
            || vectors.Any(v => v.Length != dimension || v.Any(x => !float.IsFinite(x))))
            throw new InvalidOperationException("The embedding service returned an unexpected count, empty vector or inconsistent dimension.");
        return new GeneratedEmbeddings<Embedding<float>>(vectors.Select(v => new Embedding<float>(v)
        {
            ModelId = settings.Embeddings.Model
        }));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is not null ? null : serviceType == typeof(EmbeddingGeneratorMetadata) ? metadata
            : serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() => http.Dispose();

    private static float[] ReadVector(JsonElement element)
        => element.EnumerateArray().Select(x => x.GetSingle()).ToArray();

    private static float[][] ReadApiVectors(JsonElement data, int count)
    {
        var entries = data.EnumerateArray().OrderBy(x => x.GetProperty("index").GetInt32()).ToArray();
        if (!entries.Select(x => x.GetProperty("index").GetInt32()).SequenceEqual(Enumerable.Range(0, count)))
            throw new InvalidOperationException("Invalid embedding indexes.");
        return entries.Select(x => ReadVector(x.GetProperty("embedding"))).ToArray();
    }
}
