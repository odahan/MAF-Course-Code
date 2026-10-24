using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maf.DemoConnection.Tests;

/// <summary>Verifies shared configuration precedence, safe editing and independence from binary copies.</summary>
[TestClass]
public sealed class ConfigurationTests
{
    private string directory = null!;
    private string central = null!;

    /// <summary>Creates isolated configuration files without changing the real shared profiles.</summary>
    [TestInitialize]
    public void Initialize()
    {
        directory = Path.Combine(Path.GetTempPath(), "maf-configuration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        central = Path.Combine(directory, "central.json");
        File.WriteAllText(central, """
            {
              "AI": {
                "ChatProvider": "Ollama",
                "LocalChat": { "Endpoint": "http://localhost:11434/", "Model": "central-model" },
                "ApiChat": { "Endpoint": "https://example.invalid/v1/", "Model": "", "ApiKeyEnvironmentVariable": "MAF_CONFIG_TEST_KEY" },
                "EmbeddingProvider": "Ollama",
                "Embeddings": { "Endpoint": "http://localhost:11434/", "Model": "embedding-model", "ApiKeyEnvironmentVariable": "MAF_CONFIG_TEST_EMBEDDING_KEY" },
                "TimeoutSeconds": 120
              },
              "Extension": { "Retained": true }
            }
            """);
        File.WriteAllText(Path.Combine(directory, "ai-settings.json"), """{"AI":{"LocalChat":{"Model":"stale-binary-model"}}}""");
    }

    /// <summary>Removes only this test's isolated temporary files.</summary>
    [TestCleanup]
    public void Cleanup() => Directory.Delete(directory, recursive: true);

    /// <summary>Checks the precedence contract with an independent environment variable and a CLI override.</summary>
    [TestMethod]
    public void CentralOverridesBinaryAndEnvironmentThenCliOverrideCentral()
    {
        var key = "CONFIG_TEST_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(key, "environment-value");
        try
        {
            var stored = JsonNode.Parse(File.ReadAllText(central))!.AsObject();
            stored[key] = "file-value";
            File.WriteAllText(central, stored.ToJsonString());
            var configuration = new ConfigurationBuilder().AddDemoConfiguration([], directory, central).Build();
            Assert.AreEqual("central-model", configuration["AI:LocalChat:Model"]);
            Assert.AreEqual("environment-value", configuration[key]);
            configuration = new ConfigurationBuilder().AddDemoConfiguration(["--" + key, "cli-value"], directory, central).Build();
            Assert.AreEqual("cli-value", configuration[key]);
        }
        finally { Environment.SetEnvironmentVariable(key, null); }
    }

    /// <summary>Proves that a saved setting is visible without copying or rebuilding binary defaults.</summary>
    [TestMethod]
    public void SaveIsReadOnNextLoadAndPreservesUnknownSectionsAndBackup()
    {
        var file = new DemoConfigurationFile(central);
        var previous = File.ReadAllText(central);
        file.SetValue("AI:LocalChat:Model", "new-model");
        var backup = file.Save();
        Assert.AreEqual(previous, File.ReadAllText(backup));
        Assert.IsTrue(JsonNode.Parse(File.ReadAllText(central))!["Extension"]!["Retained"]!.GetValue<bool>());
        var configuration = new ConfigurationBuilder().AddDemoConfiguration([], directory, central).Build();
        Assert.AreEqual("new-model", configuration["AI:LocalChat:Model"]);
        Assert.AreEqual("stale-binary-model", JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "ai-settings.json")))!["AI"]!["LocalChat"]!["Model"]!.GetValue<string>());
    }

    /// <summary>Rejects invalid endpoints, dimensions, providers, paths, dates and direct secret fields before writing.</summary>
    [TestMethod]
    [DataRow("AI:LocalChat:Endpoint", "https://user:secret@example.invalid/")]
    [DataRow("AI:EmbeddingDimensions", "0")]
    [DataRow("AI:TimeoutSeconds", "3601")]
    [DataRow("AI:ChatProvider", "Unknown")]
    [DataRow("MAF_STATE_ROOT", "relative/state")]
    [DataRow("Rag:AsOf", "2026-02-30")]
    [DataRow("AI:ApiChat:ApiKey", "must-not-persist")]
    [DataRow("AI:ApiChat:ApiKeyEnvironmentVariable", "BAD=NAME")]
    public void InvalidEditDoesNotMutateTheFile(string key, string value)
    {
        string before = File.ReadAllText(central);
        var file = new DemoConfigurationFile(central);
        Assert.ThrowsExactly<ArgumentException>(() => file.SetValue(key, value));
        Assert.AreEqual(before, File.ReadAllText(central));
    }

    /// <summary>Requires a cloud model only when that chat profile is selected.</summary>
    [TestMethod]
    public void CloudSelectionWithoutModelCannotOverwriteValidConfiguration()
    {
        string before = File.ReadAllText(central);
        var file = new DemoConfigurationFile(central);
        file.Validate();
        file.SetValue("AI:ChatProvider", "OpenAICompatible");
        Assert.ThrowsExactly<ArgumentException>(() => file.Save());
        Assert.AreEqual(before, File.ReadAllText(central));
        file.SetValue("AI:ApiChat:Model", "cloud-model");
        file.Save();
        Assert.AreEqual("Ollama", file.GetValue("AI:EmbeddingProvider"));
    }

    /// <summary>Protects settings saved by another editor after the current document was opened.</summary>
    [TestMethod]
    public void StaleEditorCannotOverwriteAnotherSave()
    {
        var first = new DemoConfigurationFile(central);
        var stale = new DemoConfigurationFile(central);
        first.SetValue("AI:LocalChat:Model", "first-wins");
        first.Save();
        stale.SetValue("AI:LocalChat:Model", "stale-value");
        Assert.ThrowsExactly<IOException>(() => stale.Save());
        Assert.AreEqual("first-wins", new DemoConfigurationFile(central).GetValue("AI:LocalChat:Model"));
    }

    /// <summary>Preserves typed numeric configuration and allows clearing optional dimensions.</summary>
    [TestMethod]
    public void OptionalDimensionsCanBeSetAndCleared()
    {
        var file = new DemoConfigurationFile(central);
        file.SetValue("AI:EmbeddingDimensions", "768");
        file.Save();
        Assert.AreEqual(768, JsonNode.Parse(File.ReadAllText(central))!["AI"]!["EmbeddingDimensions"]!.GetValue<int>());
        file.SetValue("AI:EmbeddingDimensions", "");
        file.Save();
        Assert.IsNull(JsonNode.Parse(File.ReadAllText(central))!["AI"]!["EmbeddingDimensions"]);
    }
}
