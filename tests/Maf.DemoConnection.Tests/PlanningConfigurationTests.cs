using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.DemoConnection.Tests;

/// <summary>Exercises the actual retained planning executables against a controlled centrally configured endpoint.</summary>
[TestClass]
public sealed class PlanningConfigurationTests
{
    /// <summary>Proves endpoint and model selection in separate processes, without any model service or business effect.</summary>
    [TestMethod]
    [DataRow("Demo260_MultiAgentRoles", "Lab04_MultiAgents.dll")]
    [DataRow("Demo270_Planning", "Lab05_PlanningPattern.dll")]
    [DataRow("Demo280_ValidatedPlanning", "Lab05_PlanningPattern.dll")]
    [DataRow("Demo290_FormattedPlanning", "Lab05_PlanningPattern_Validate_Format.dll")]
    public async Task PlanningExecutableUsesCentralEndpointAndModel(string folder, string assembly)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "BaseTechnique.slnx"))) root = root.Parent;
        Assert.IsNotNull(root);
        string configurationName = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        string executable = Path.Combine(root.FullName, "demos", folder, "bin", configurationName, "net10.0", assembly);
        Assert.IsTrue(File.Exists(executable), "Build the canonical solution before running executable integration tests.");

        var directory = Path.Combine(Path.GetTempPath(), "maf-planning-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        int port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        using var listener = new HttpListener();
        string endpoint = $"http://127.0.0.1:{port}/";
        listener.Prefixes.Add(endpoint);
        listener.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var process = new Process();
        bool started = false;
        try
        {
            var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(root.FullName, "shared", "Configuration", "ai-settings.json")))!;
            settings["AI"]!["ChatProvider"] = "Ollama";
            settings["AI"]!["LocalChat"]!["Endpoint"] = endpoint;
            settings["AI"]!["LocalChat"]!["Model"] = "configured-planning-fixture";
            string file = Path.Combine(directory, "ai-settings.json");
            File.WriteAllText(file, settings.ToJsonString());
            process.StartInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Path.GetTempPath(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            process.StartInfo.ArgumentList.Add(executable);
            process.StartInfo.Environment["MAF_DEMO_CONFIG"] = file;
            foreach (var key in new[] { "AI__ChatProvider", "AI__LocalChat__Endpoint", "AI__LocalChat__Model",
                "AI:ChatProvider", "AI:LocalChat:Endpoint", "AI:LocalChat:Model" })
                process.StartInfo.Environment.Remove(key);
            process.Start();
            started = true;
            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.StandardInput.WriteLineAsync("Explain a programming concept.");
            process.StandardInput.Close();

            var context = await listener.GetContextAsync().WaitAsync(timeout.Token);
            Assert.AreEqual("/api/chat", context.Request.Url!.AbsolutePath);
            using var body = await JsonDocument.ParseAsync(context.Request.InputStream, cancellationToken: timeout.Token);
            Assert.AreEqual("configured-planning-fixture", body.RootElement.GetProperty("model").GetString());
            const string answer = """{"steps":[{"type":"respond","argument":"Configured endpoint receipt"}]}""";
            byte[] response = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                model = "configured-planning-fixture",
                message = new { role = "assistant", content = answer },
                done = true
            }));
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = response.Length;
            await context.Response.OutputStream.WriteAsync(response, timeout.Token);
            context.Response.Close();
            await process.WaitForExitAsync(timeout.Token);
            string output = await stdout;
            string error = await stderr;
            Assert.AreEqual(0, process.ExitCode, error);
            StringAssert.Contains(output, "Configured endpoint receipt");
        }
        finally
        {
            listener.Stop();
            if (started && !process.HasExited) process.Kill(entireProcessTree: true);
            Directory.Delete(directory, recursive: true);
        }
    }
}
