using System.Net;
using System.Text;
using System.Text.Json;
using Maf.DemoConnection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.DemoConnection.Tests;

/// <summary>Verifies transport behavior without a model or a paid service.</summary>
[TestClass]
public sealed class ConnectionTests
{
    /// <summary>Ensures both adapters send a native schema and tool definition without a prompt fallback.</summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task NativeSchemaAndToolsReachBothProtocols(bool local)
    {
        var variable = "MAF_TEST_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(variable, "test-secret");
        try
        {
            var handler = new RecordingHandler(local
                ? """{"model":"granite4:3b","message":{"role":"assistant","content":"{}"},"done":true}"""
                : """{"id":"test","object":"chat.completion","created":0,"model":"api-model","choices":[{"index":0,"message":{"role":"assistant","content":"{}"},"finish_reason":"stop"}]}""");
            var settings = new ConnectionOptions
            {
                ApiChat = new() { Endpoint = "https://example.invalid/v1/", Model = "api-model", ApiKeyEnvironmentVariable = variable }
            };
            using var chat = DemoConnection.CreateChatClient(settings, local, handler);
            await chat.GetResponseAsync("Classify.", new()
            {
                ResponseFormat = ChatResponseFormat.ForJsonSchema<SchemaProbe>(),
                Tools = [AIFunctionFactory.Create(() => "receipt", "ReadReceipt")]
            });
            using var body = JsonDocument.Parse(handler.Body!);
            var root = body.RootElement;
            var schema = local ? root.GetProperty("format")
                : root.GetProperty("response_format").GetProperty("json_schema").GetProperty("schema");
            Assert.IsTrue(schema.GetProperty("properties").EnumerateObject().Any(p => p.Name.Equals("Action", StringComparison.OrdinalIgnoreCase)));
            Assert.AreEqual("ReadReceipt", root.GetProperty("tools")[0].GetProperty("function").GetProperty("name").GetString());
        }
        finally { Environment.SetEnvironmentVariable(variable, null); }
    }

    /// <summary>Provides a small serializable shape for transport assertions.</summary>
    public sealed record SchemaProbe(string Action);

    /// <summary>Checks local default routing, model selection and transport ownership.</summary>
    [TestMethod]
    public async Task LocalChatUsesConfiguredModelAndDisposesTransport()
    {
        var handler = new RecordingHandler("""{"model":"granite4:3b","message":{"role":"assistant","content":"Hello"},"done":true}""");
        using (var chat = DemoConnection.CreateChatClient(handler: handler))
        {
            Assert.AreEqual("Hello", (await chat.GetResponseAsync("Hi")).Text);
            Assert.AreEqual("/api/chat", handler.Uri!.AbsolutePath);
            Assert.AreEqual("granite4:3b", JsonDocument.Parse(handler.Body!).RootElement.GetProperty("model").GetString());
            Assert.IsNull(handler.Authorization);
        }
        Assert.IsTrue(handler.Disposed);
    }

    /// <summary>Checks API path, authorization, model and the boolean facade.</summary>
    [TestMethod]
    public async Task ApiChatUsesKeyEndpointAndModel()
    {
        var variable = "MAF_TEST_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(variable, "test-secret");
        try
        {
            var handler = new RecordingHandler("""{"id":"test","object":"chat.completion","created":0,"model":"api-model","choices":[{"index":0,"message":{"role":"assistant","content":"Hello API"},"finish_reason":"stop"}]}""");
            var settings = new ConnectionOptions
            {
                ApiChat = new() { Endpoint = "https://example.invalid/custom/v1", Model = "api-model", ApiKeyEnvironmentVariable = variable }
            };
            using (var chat = DemoConnection.CreateChatClient(settings, isLocal: false, handler))
            {
                Assert.AreEqual("Hello API", (await chat.GetResponseAsync("Hi")).Text);
                Assert.AreEqual("/custom/v1/chat/completions", handler.Uri!.AbsolutePath);
                Assert.AreEqual("Bearer test-secret", handler.Authorization);
                Assert.AreEqual("api-model", JsonDocument.Parse(handler.Body!).RootElement.GetProperty("model").GetString());
            }
            Assert.IsTrue(handler.Disposed);
        }
        finally { Environment.SetEnvironmentVariable(variable, null); }
    }

    /// <summary>Checks streaming through the maintained adapter.</summary>
    [TestMethod]
    public async Task LocalChatStreamsUpdates()
    {
        var handler = new RecordingHandler("{\"model\":\"granite4:3b\",\"message\":{\"role\":\"assistant\",\"content\":\"Hello\"},\"done\":false}\n{\"model\":\"granite4:3b\",\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"done\":true}\n");
        using var chat = DemoConnection.CreateChatClient(handler: handler);
        var text = new StringBuilder();
        await foreach (var update in chat.GetStreamingResponseAsync("Hi"))
            text.Append(update.Text);
        Assert.AreEqual("Hello", text.ToString());
        Assert.IsTrue(JsonDocument.Parse(handler.Body!).RootElement.GetProperty("stream").GetBoolean());
    }

    /// <summary>Checks API streaming, cancellation and provider failures without fallback or retries.</summary>
    [TestMethod]
    public async Task ApiStreamingCancellationAndFailureUseOneTransport()
    {
        var variable = "MAF_TEST_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(variable, "test-secret");
        try
        {
            var settings = new ConnectionOptions
            {
                ChatProvider = ConnectionProvider.OpenAICompatible,
                ApiChat = new() { Endpoint = "https://example.invalid/v1/", Model = "api-model", ApiKeyEnvironmentVariable = variable }
            };
            var streaming = new RecordingHandler("data: {\"id\":\"test\",\"object\":\"chat.completion.chunk\",\"created\":0,\"model\":\"api-model\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\",\"content\":\"Hello\"}}]}\n\ndata: [DONE]\n\n", mediaType: "text/event-stream");
            using (var chat = DemoConnection.CreateChatClient(settings, handler: streaming))
            {
                var text = new StringBuilder();
                await foreach (var update in chat.GetStreamingResponseAsync("Hi"))
                    text.Append(update.Text);
                Assert.AreEqual("Hello", text.ToString());
            }
            var waiting = new RecordingHandler("{}") { WaitForCancellation = true };
            using (var chat = DemoConnection.CreateChatClient(settings, handler: waiting))
            using (var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
                await Assert.ThrowsAsync<OperationCanceledException>(() => chat.GetResponseAsync("Hi", cancellationToken: cancellation.Token));
            Assert.AreEqual(1, waiting.Calls);
            var failure = new RecordingHandler("{}", HttpStatusCode.ServiceUnavailable);
            using (var chat = DemoConnection.CreateChatClient(settings, handler: failure))
                await Assert.ThrowsAsync<System.ClientModel.ClientResultException>(() => chat.GetResponseAsync("Hi"));
            Assert.AreEqual(1, failure.Calls);
            Assert.AreEqual("example.invalid", failure.Uri!.Host);
        }
        finally { Environment.SetEnvironmentVariable(variable, null); }
    }

    /// <summary>Checks that cloud chat does not switch local embeddings or require a chat key.</summary>
    [TestMethod]
    public async Task EmbeddingsAreBatchedIndependentAndNeverSilentlyTruncated()
    {
        var handler = new RecordingHandler("""{"embeddings":[[1,2],[3,4]]}""");
        using var generator = DemoConnection.CreateEmbeddingGenerator(new() { ChatProvider = ConnectionProvider.OpenAICompatible }, handler);
        var result = await generator.GenerateAsync(["first", "second"]);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(2, result[0].Vector.Length);
        Assert.AreEqual("/api/embed", handler.Uri!.AbsolutePath);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.AreEqual("embeddinggemma:300m", body.RootElement.GetProperty("model").GetString());
        Assert.AreEqual(2, body.RootElement.GetProperty("input").GetArrayLength());
        Assert.IsFalse(body.RootElement.GetProperty("truncate").GetBoolean());
    }

    /// <summary>Rejects malformed counts, vectors, dimensions and indexes before index ingestion.</summary>
    [TestMethod]
    [DataRow("{\"embeddings\":[]}")]
    [DataRow("{\"embeddings\":[[]]}")]
    [DataRow("{\"embeddings\":[[1,2,3]]}")]
    [DataRow("{\"embedding\":[1,2]}")]
    [DataRow("{\"embeddings\":[[1,2],[3]]}")]
    public async Task InvalidEmbeddingResponsesFail(string response)
    {
        using var generator = DemoConnection.CreateEmbeddingGenerator(new() { EmbeddingDimensions = 2 }, new RecordingHandler(response));
        await Assert.ThrowsAsync<InvalidOperationException>(() => generator.GenerateAsync(["text"]));
    }

    /// <summary>Checks the independent API embedding profile and response index ordering.</summary>
    [TestMethod]
    public async Task ApiEmbeddingsUseTheirOwnProfileAndOrder()
    {
        var variable = "MAF_TEST_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(variable, "embedding-secret");
        try
        {
            var handler = new RecordingHandler("""{"data":[{"index":1,"embedding":[3,4]},{"index":0,"embedding":[1,2]}]}""");
            using var generator = DemoConnection.CreateEmbeddingGenerator(new()
            {
                EmbeddingProvider = ConnectionProvider.OpenAICompatible,
                Embeddings = new() { Endpoint = "https://example.invalid/v1/", Model = "embed-model", ApiKeyEnvironmentVariable = variable }
            }, handler);
            var result = await generator.GenerateAsync(["first", "second"]);
            Assert.AreEqual(1f, result[0].Vector.Span[0]);
            Assert.AreEqual("/v1/embeddings", handler.Uri!.AbsolutePath);
            Assert.AreEqual("Bearer embedding-secret", handler.Authorization);
            Assert.AreEqual("embed-model", result[0].ModelId);
        }
        finally { Environment.SetEnvironmentVariable(variable, null); }
    }

    /// <summary>Verifies configuration errors never include invalid values or secrets.</summary>
    [TestMethod]
    public void ConfigurationErrorsAreExplicitAndRedacted()
    {
        var error = Assert.Throws<ArgumentException>(() => DemoConnection.CreateChatClient(new()
        {
            LocalChat = new() { Endpoint = "https://user:supersecret@example.invalid/?key=supersecret" }
        }));
        Assert.IsFalse(error.ToString().Contains("supersecret"));
        Assert.Throws<ArgumentException>(() => DemoConnection.CreateChatClient(new(), isLocal: false));
        Assert.Throws<ArgumentException>(() => DemoConnection.CreateChatClient(new() { TimeoutSeconds = 0 }));
        Assert.Throws<ArgumentException>(() => DemoConnection.CreateChatClient(new() { ChatProvider = (ConnectionProvider)99 }));
        Assert.Throws<ArgumentException>(() => DemoConnection.CreateEmbeddingGenerator(new() { EmbeddingDimensions = 0 }));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI:ChatProvider"] = "secret-invalid-provider"
        }).Build();
        error = Assert.Throws<ArgumentException>(() => ConnectionOptions.FromConfiguration(configuration));
        Assert.IsFalse(error.ToString().Contains("secret-invalid-provider"));
    }

    /// <summary>Checks provider selection from configuration and a local override of that selection.</summary>
    [TestMethod]
    public void ConfigurationSelectionAndMissingSecretsFailBeforeNetwork()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI:ChatProvider"] = "OpenAICompatible",
            ["AI:ApiChat:Model"] = "configured-model",
            ["AI:ApiChat:ApiKeyEnvironmentVariable"] = "MAF_MISSING_" + Guid.NewGuid().ToString("N")
        }).Build();
        var settings = ConnectionOptions.FromConfiguration(configuration);
        var handler = new RecordingHandler("{}");
        Assert.Throws<ArgumentException>(() => DemoConnection.CreateChatClient(settings, handler: handler));
        Assert.AreEqual(0, handler.Calls);
        using var local = DemoConnection.CreateChatClient(settings, isLocal: true, handler);
    }

    /// <summary>Checks cancellation reaches both transport types.</summary>
    [TestMethod]
    public async Task ChatAndEmbeddingCancellationReachTransport()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var chatHandler = new RecordingHandler("{}") { WaitForCancellation = true };
        using var chat = DemoConnection.CreateChatClient(handler: chatHandler);
        await Assert.ThrowsAsync<OperationCanceledException>(() => chat.GetResponseAsync("Hi", cancellationToken: cancellation.Token));
        using var embeddingCancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var embeddingHandler = new RecordingHandler("{}") { WaitForCancellation = true };
        using var generator = DemoConnection.CreateEmbeddingGenerator(handler: embeddingHandler);
        await Assert.ThrowsAsync<OperationCanceledException>(() => generator.GenerateAsync(["text"], cancellationToken: embeddingCancellation.Token));
        Assert.AreEqual(1, chatHandler.Calls);
        Assert.AreEqual(1, embeddingHandler.Calls);
    }

    /// <summary>Checks timeout and failure do not trigger a provider fallback.</summary>
    [TestMethod]
    public async Task TimeoutAndLocalFailuresDoNotFallback()
    {
        var handler = new RecordingHandler("{}") { WaitForCancellation = true };
        using var chat = DemoConnection.CreateChatClient(new() { TimeoutSeconds = 1 }, handler: handler);
        await Assert.ThrowsAsync<OperationCanceledException>(() => chat.GetResponseAsync("Hi"));
        Assert.AreEqual(1, handler.Calls);
        var failure = new RecordingHandler("sensitive service diagnostic", HttpStatusCode.ServiceUnavailable);
        using var generator = DemoConnection.CreateEmbeddingGenerator(handler: failure);
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => generator.GenerateAsync(["text"]));
        Assert.IsFalse(error.ToString().Contains("sensitive service diagnostic"));
        Assert.AreEqual(1, failure.Calls);
    }

    /// <summary>Checks unsupported options fail explicitly and host ownership disposes clients.</summary>
    [TestMethod]
    public async Task UnsupportedOptionsAndDisposalAreExplicit()
    {
        var handler = new RecordingHandler("{}");
        using (var generator = DemoConnection.CreateEmbeddingGenerator(handler: handler))
        {
            await Assert.ThrowsAsync<NotSupportedException>(() => generator.GenerateAsync(["text"], new() { ModelId = "other" }));
            Assert.AreEqual(0, handler.Calls);
        }
        Assert.IsTrue(handler.Disposed);
        var services = new ServiceCollection().AddDemoConnection(new());
        var provider = services.BuildServiceProvider();
        var chat = provider.GetRequiredService<IChatClient>();
        Assert.AreSame(chat, provider.GetRequiredService<IChatClient>());
        provider.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => chat.GetResponseAsync("Hi"));
    }

    private sealed class RecordingHandler(string response, HttpStatusCode status = HttpStatusCode.OK,
        string mediaType = "application/json") : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        public string? Body { get; private set; }
        public string? Authorization { get; private set; }
        public int Calls { get; private set; }
        public bool Disposed { get; private set; }
        public bool WaitForCancellation { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Uri = request.RequestUri;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Authorization = request.Headers.Authorization?.ToString();
            if (WaitForCancellation)
                await Task.Delay(Timeout.Infinite, cancellationToken);
            return new(status) { Content = new StringContent(response, Encoding.UTF8, mediaType) };
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
