using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Streaming.Client;
using Streaming.Server;

namespace Streaming.Tests;

/// <summary>Exercises Kestrel, native MAF sessions and the production HTTP client without an inference service.</summary>
[TestClass]
public sealed class StreamingTests
{
    [TestMethod]
    [DataRow("{\"type\":\"text\",\"text\":\"世界🙂 \\n\"}\n{\"type\":\"completed\"}\n", true)]
    [DataRow("{\"type\":\"text\",\"text\":\"partial\"}\n", false)]
    public async Task ClientHandlesSplitUtf8AndRejectsMissingCompletion(string wire, bool complete)
    {
        using var http = new HttpClient(new WireHandler(wire)) { BaseAddress = new Uri("http://localhost") };
        var result = "";
        var run = new DiscussionClient(http).StreamAsync(new(Guid.NewGuid().ToString("N"), "Test"), value => result += value);
        if (complete)
        {
            await run;
            Assert.AreEqual("世界🙂 \n", result);
        }
        else await Assert.ThrowsExactlyAsync<EndOfStreamException>(() => run);
    }

    [TestMethod]
    public async Task ExactWhitespaceAndUnicodeSurviveHttpAndSessionRestart()
    {
        var model = new ScriptedModel();
        await using var host = await Host.StartAsync(model);
        var id = Guid.NewGuid().ToString("N");
        var text = new StringBuilder();
        await host.Client.StreamAsync(new(id, "Remember marker ORCHID"), value => text.Append(value));
        Assert.AreEqual("Hello \n\t世界🙂", text.ToString());
        Assert.IsTrue(File.Exists(Path.Combine(host.Root, id + ".json")));
        await using var restarted = await Host.StartAsync(model, host.Root);
        await restarted.Client.StreamAsync(new(id, "Recall marker"), _ => { });
        Assert.IsTrue(model.Requests.Last().Any(m => m.Text.Contains("ORCHID", StringComparison.Ordinal)));
        await restarted.Client.StreamAsync(new(Guid.NewGuid().ToString("N"), "Isolated"), _ => { });
        Assert.IsFalse(model.Requests.Last().Any(m => m.Text.Contains("ORCHID", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task CompleteAndStreamUseTheSameStateAndText()
    {
        await using var host = await Host.StartAsync(new());
        var id = Guid.NewGuid().ToString("N");
        using var response = await host.Http.PostAsJsonAsync("/discussion", new DiscussionRequest(id, "First turn"));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("Hello \n\t世界🙂", body.GetProperty("message").GetString());
        await host.Client.StreamAsync(new(id, "Second turn"), _ => { });
        Assert.AreEqual(1, Directory.GetFiles(host.Root, "*.json").Length);
    }

    [TestMethod]
    public async Task MidstreamErrorIsVisibleAndDoesNotCommitPartialTurn()
    {
        var model = new ScriptedModel();
        await using var host = await Host.StartAsync(model);
        var id = Guid.NewGuid().ToString("N");
        await host.Client.StreamAsync(new(id, "Successful"), _ => { });
        var snapshot = await File.ReadAllTextAsync(Path.Combine(host.Root, id + ".json"));
        model.Fail = true;
        var partial = "";
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => host.Client.StreamAsync(new(id, "Fail"), s => partial += s));
        Assert.AreEqual("Hello", partial);
        Assert.AreEqual(snapshot, await File.ReadAllTextAsync(Path.Combine(host.Root, id + ".json")));
        model.Fail = false;
        await host.Client.StreamAsync(new(id, "Retry"), _ => { });
        Assert.IsFalse(model.Requests.Last().Any(m => m.Text == "Fail"));
    }

    [TestMethod]
    public async Task ClientCancellationReachesModelAndDoesNotSave()
    {
        var model = new ScriptedModel { Block = true };
        await using var host = await Host.StartAsync(model);
        using var stop = new CancellationTokenSource();
        var request = host.Client.StreamAsync(new(Guid.NewGuid().ToString("N"), "Block"), _ => stop.Cancel(), stop.Token);
        await Assert.ThrowsAsync<OperationCanceledException>(() => request);
        await model.Canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreEqual(0, Directory.GetFiles(host.Root, "*.json").Length);
    }

    [TestMethod]
    public async Task ConcurrentGenerationIsRejectedAndGateRecovers()
    {
        var model = new ScriptedModel { Block = true };
        await using var host = await Host.StartAsync(model);
        using var stop = new CancellationTokenSource();
        var first = host.Client.StreamAsync(new(Guid.NewGuid().ToString("N"), "Hold"), _ => { }, stop.Token);
        await model.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        using var second = await host.Http.PostAsJsonAsync("/discussion", new DiscussionRequest(Guid.NewGuid().ToString("N"), "Other"));
        Assert.AreEqual(HttpStatusCode.Conflict, second.StatusCode);
        stop.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => first);
        await model.Canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [TestMethod]
    [DataRow("../escape", "Hello")]
    [DataRow("", "Hello")]
    [DataRow("01234567890123456789012345678901", " ")]
    public async Task InvalidRequestsFailBeforeInference(string id, string message)
    {
        var model = new ScriptedModel();
        await using var host = await Host.StartAsync(model);
        using var response = await host.Http.PostAsJsonAsync("/discussion/flux", new DiscussionRequest(id, message));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(0, model.Requests.Count);
    }

    [TestMethod]
    public async Task CorsAllowsOnlyConfiguredLocalOrigin()
    {
        await using var host = await Host.StartAsync(new());
        foreach (var origin in new[] { "http://localhost:5089", "https://untrusted.example" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, "/discussion/flux");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "content-type");
            using var response = await host.Http.SendAsync(request);
            Assert.AreEqual(origin == "http://localhost:5089", response.Headers.Contains("Access-Control-Allow-Origin"));
        }
    }

    [TestMethod]
    [DataRow("INC-318", "InProgress")]
    [DataRow("INC-999", "NotFound")]
    public async Task RealMcpLookupSuppliesActualIncidentDataToAgent(string id, string expected)
    {
        var model = new ScriptedModel();
        await using var host = await Host.StartAsync(model);
        await host.Client.StreamAsync(new(Guid.NewGuid().ToString("N"), "/incident " + id), _ => { });
        var prompt = model.Requests.Single().Last().Text;
        StringAssert.Contains(prompt, expected);
    }

    [TestMethod]
    public async Task CorruptSessionFailsWithoutOverwritingIt()
    {
        await using var host = await Host.StartAsync(new());
        var id = Guid.NewGuid().ToString("N");
        var path = Path.Combine(host.Root, id + ".json");
        await File.WriteAllTextAsync(path, "corrupt");
        await Assert.ThrowsAsync<HttpRequestException>(() => host.Client.StreamAsync(new(id, "Hello"), _ => { }));
        Assert.AreEqual("corrupt", await File.ReadAllTextAsync(path));
    }

    [TestMethod]
    public async Task ToolkitUpdatesOneMessageAndCreatesANewConversation()
    {
        await using var host = await Host.StartAsync(new());
        var vm = new DiscussionViewModel(host.Client) { Input = "Hello" };
        await vm.SendCommand.ExecuteAsync(null);
        Assert.AreEqual("Completed and saved", vm.Status);
        Assert.AreEqual(2, vm.Messages.Count);
        Assert.AreEqual("Hello \n\t世界🙂", vm.Messages[1].Text);
        vm.NewConversationCommand.Execute(null);
        Assert.AreEqual(0, vm.Messages.Count);
    }

    [TestMethod]
    public async Task ToolkitCancelStopsTheServerAndPreventsConcurrentSend()
    {
        var model = new ScriptedModel { Block = true };
        await using var host = await Host.StartAsync(model);
        var vm = new DiscussionViewModel(host.Client) { Input = "Hold" };
        var pending = vm.SendCommand.ExecuteAsync(null);
        await model.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.IsFalse(vm.SendCommand.CanExecute(null));
        vm.SendCancelCommand.Execute(null);
        await pending;
        await model.Canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
        StringAssert.StartsWith(vm.Status, "Canceled");
        Assert.IsTrue(vm.SendCommand.CanExecute(null));
        Assert.AreEqual(0, Directory.GetFiles(host.Root, "*.json").Length);
    }

    private sealed class Host(WebApplication app, HttpClient http, string root) : IAsyncDisposable
    {
        public string Root { get; } = root;
        public HttpClient Http { get; } = http;
        public DiscussionClient Client { get; } = new(http);
        public static async Task<Host> StartAsync(ScriptedModel model, string? root = null)
        {
            root ??= Path.Combine(Path.GetTempPath(), "maf-streaming", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var server = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "../../../../../demos/Demo400_LocalMcp/Server/bin", new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name,
                "net10.0/ServeurMcpLocal.dll"));
            var app = Backend.Create(["--urls", "http://127.0.0.1:0"], builder =>
            {
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Conversation:StateRoot"] = root, ["Mcp:ServerPath"] = server
                });
                builder.Services.AddSingleton<IChatClient>(model);
            });
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            return new(app, new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(20) }, root);
        }
        public async ValueTask DisposeAsync() { Http.Dispose(); await app.StopAsync(); await app.DisposeAsync(); }
    }

    private sealed class WireHandler(string wire) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new FragmentedStream(Encoding.UTF8.GetBytes(wire)))
            });
    }

    private sealed class FragmentedStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
            => base.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], token);
    }

    private sealed class ScriptedModel : IChatClient
    {
        public bool Fail { get; set; }
        public bool Block { get; set; }
        public List<List<ChatMessage>> Requests { get; } = [];
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Requests.Add(messages.ToList());
            Started.TrySetResult();
            yield return new(ChatRole.Assistant, "Hello");
            if (Fail) throw new IOException("Internal secret must not appear in public errors");
            if (Block)
            {
                try { await Task.Delay(Timeout.Infinite, cancellationToken); }
                finally { if (cancellationToken.IsCancellationRequested) Canceled.TrySetResult(); }
            }
            foreach (var fragment in new[] { " ", "\n", "\t", "世界", "🙂" })
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                yield return new(ChatRole.Assistant, fragment);
            }
        }
        public object? GetService(Type serviceType, object? serviceKey = null) => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
        public void Dispose() { }
    }
}

