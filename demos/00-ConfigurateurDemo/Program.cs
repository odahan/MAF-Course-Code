using System.Net.Http.Json;
using System.Text.Json;
using Maf.DemoConnection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Spectre.Console;

return await Configurator.RunAsync(args);

/// <summary>Provides an interactive central editor and scriptable diagnostics without persisting API keys in JSON.</summary>
internal static class Configurator
{
    /// <summary>Runs the menu or a non-interactive command. Network calls occur only for explicit diagnostics.</summary>
    public static async Task<int> RunAsync(string[] arguments)
    {
        try
        {
            var args = arguments.ToList();
            string? explicitFile = null;
            int index = args.IndexOf("--config");
            if (index >= 0)
            {
                if (index + 1 >= args.Count) throw new ArgumentException("--config requires a file path.");
                explicitFile = Path.GetFullPath(args[index + 1]);
                args.RemoveRange(index, 2);
            }
            if (args.Count == 1 && args[0] is "--help" or "help")
            {
                AnsiConsole.WriteLine("00-ConfigurateurDemo [--config <file>] [show | validate | set <key> <value> | models [chat|embeddings] | check chat|embeddings]");
                AnsiConsole.WriteLine("Without a command: interactive editor. API keys are never accepted as command arguments.");
                return 0;
            }
            var path = explicitFile ?? DemoConfiguration.FindCentralFile()
                ?? Path.Combine(AppContext.BaseDirectory, "ai-settings.json");
            var file = new DemoConfigurationFile(path);
            if (args.Count == 0)
            {
                if (Console.IsInputRedirected)
                    throw new ArgumentException("An interactive terminal is required. Use show, validate or set in scripts.");
                return await RunMenuAsync(file);
            }

            switch (args[0])
            {
                case "show" when args.Count == 1:
                    Show(file);
                    return 0;
                case "validate" when args.Count == 1:
                    file.Validate();
                    AnsiConsole.MarkupLine("[green]Stored configuration is valid.[/] Network connectivity and API keys were not tested.");
                    return 0;
                case "set" when args.Count == 3:
                    file.SetValue(args[1], args[2]);
                    Save(file);
                    return 0;
                case "models" when args.Count is 1 or 2:
                    await ListModelsAsync(file, args.Count == 2 ? args[1] : "chat");
                    return 0;
                case "check" when args.Count == 2:
                    await CheckAsync(file, args[1]);
                    return 0;
                default:
                    throw new ArgumentException("Unknown command or argument count. Run with --help.");
            }
        }
        catch (Exception error) when (error is ArgumentException or IOException or JsonException or InvalidOperationException
            or HttpRequestException or OperationCanceledException or UnauthorizedAccessException)
        {
            AnsiConsole.MarkupLine("[red]Configuration operation failed.[/]");
            AnsiConsole.WriteLine(error is ArgumentException ? error.Message : error.GetType().Name);
            AnsiConsole.WriteLine("The existing configuration is preserved when validation or connectivity fails. API keys are never printed.");
            return 1;
        }
    }

    private static async Task<int> RunMenuAsync(DemoConfigurationFile file)
    {
        bool dirty = false;
        AnsiConsole.Write(new Rule("00 — Demo Configurator"));
        AnsiConsole.WriteLine(file.FilePath);
        AnsiConsole.WriteLine("Changes apply to newly started demos after saving. Restart running backends. Environment and CLI overrides take precedence.");
        while (true)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title(dirty ? "[yellow]Unsaved changes[/] — choose an action" : "Choose an action")
                .PageSize(15).AddChoices("View settings", "Active chat provider", "Ollama profile", "Cloud profile", "Embedding profile",
                    "Other settings", "Installed Ollama models", "Store API key", "Test saved chat profile", "Test saved embedding profile", "Save", "Exit"));
            try
            {
                switch (choice)
                {
                    case "View settings": Show(file); break;
                    case "Active chat provider": Edit(file, "Chat"); dirty = true; break;
                    case "Ollama profile": Edit(file, "Ollama"); dirty = true; break;
                    case "Cloud profile": Edit(file, "Cloud"); dirty = true; break;
                    case "Embedding profile": Edit(file, "Embeddings"); dirty = true; break;
                    case "Other settings": Edit(file, null); dirty = true; break;
                    case "Installed Ollama models":
                        await ListModelsAsync(file, AnsiConsole.Prompt(new SelectionPrompt<string>()
                            .Title("Use which saved Ollama endpoint?").AddChoices("chat", "embeddings")));
                        break;
                    case "Store API key": StoreSecret(file); break;
                    case "Test saved chat profile": await CheckAsync(file, "chat"); break;
                    case "Test saved embedding profile": await CheckAsync(file, "embeddings"); break;
                    case "Save": Save(file); dirty = false; break;
                    case "Exit":
                        if (!dirty || AnsiConsole.Confirm("Discard unsaved changes?", false)) return 0;
                        break;
                }
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                AnsiConsole.MarkupLine("[red]Operation failed.[/]");
                AnsiConsole.WriteLine(error is ArgumentException ? error.Message : error.GetType().Name);
            }
        }
    }

    private static void Edit(DemoConfigurationFile file, string? group)
    {
        var fields = DemoConfigurationFile.Fields.Where(field => group is null
            ? field.Group is not ("Chat" or "Ollama" or "Cloud" or "Embeddings") : field.Group == group).ToArray();
        var selected = AnsiConsole.Prompt(new SelectionPrompt<ConfigurationField>().Title("Select a setting")
            .PageSize(15).UseConverter(field => Markup.Escape(field.Key + " — " + field.Description)).AddChoices(fields));
        string value;
        if (selected.Kind == "provider")
            value = AnsiConsole.Prompt(new SelectionPrompt<string>().Title(Markup.Escape(selected.Description))
                .AddChoices("Ollama", "OpenAICompatible"));
        else
        {
            AnsiConsole.WriteLine(selected.Description);
            AnsiConsole.WriteLine("Current stored value: " + file.GetValue(selected.Key));
            value = AnsiConsole.Prompt(new TextPrompt<string>("New value (blank clears an optional setting):").AllowEmpty());
        }
        file.SetValue(selected.Key, value);
        if (selected.Key.StartsWith("AI:Embedding", StringComparison.Ordinal))
            AnsiConsole.MarkupLine("[yellow]Rebuild existing vector indexes after changing the embedding model, provider or dimensions.[/]");
    }

    private static void Show(DemoConfigurationFile file)
    {
        using var effective = Effective(file);
        var table = new Table().AddColumn("Setting").AddColumn("Stored value").AddColumn("Effective saved value");
        foreach (var field in DemoConfigurationFile.Fields)
            table.AddRow(new Text(field.Key), new Text(file.GetValue(field.Key)), new Text(effective[field.Key] ?? "(default)"));
        AnsiConsole.WriteLine("Central file: " + file.FilePath);
        AnsiConsole.Write(table);
        foreach (var key in new[] { "AI:ApiChat:ApiKeyEnvironmentVariable", "AI:Embeddings:ApiKeyEnvironmentVariable" })
        {
            var variable = effective[key];
            AnsiConsole.WriteLine(key + ": " + variable + " — "
                + (!string.IsNullOrWhiteSpace(variable) && !string.IsNullOrWhiteSpace(DemoConfiguration.ReadSecret(variable)) ? "present" : "missing"));
        }
        AnsiConsole.WriteLine("Effective values include environment overrides. Individual demo command-line arguments can override them again.");
    }

    private static ConfigurationRoot Effective(DemoConfigurationFile file) =>
        (ConfigurationRoot)new ConfigurationBuilder().AddDemoConfiguration([], centralFile: file.FilePath).Build();

    private static void Save(DemoConfigurationFile file)
    {
        string backup = file.Save();
        AnsiConsole.MarkupLine("[green]Configuration saved.[/]");
        AnsiConsole.WriteLine("Backup: " + backup);
        AnsiConsole.WriteLine("Newly started demos read this central file without rebuilding. Restart running demos/backends.");
    }

    private static void StoreSecret(DemoConfigurationFile file)
    {
        if (!OperatingSystem.IsWindows()) throw new ArgumentException("Set the configured API key environment variable in your shell on this platform.");
        var target = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select an API key").AddChoices("Cloud chat", "Embeddings"));
        var key = target == "Cloud chat" ? "AI:ApiChat:ApiKeyEnvironmentVariable" : "AI:Embeddings:ApiKeyEnvironmentVariable";
        var variable = file.GetValue(key);
        if (string.IsNullOrWhiteSpace(variable)) throw new ArgumentException("Configure the environment variable name first.");
        if (!variable.StartsWith("MAF_", StringComparison.Ordinal) && !variable.EndsWith("_API_KEY", StringComparison.Ordinal))
            throw new ArgumentException("Saving a key requires a MAF_ variable or a name ending in _API_KEY. Other variable names can be supplied externally.");
        AnsiConsole.WriteLine("The key will be stored in your Windows user environment, not in the JSON or its backups.");
        var secret = AnsiConsole.Prompt(new TextPrompt<string>("API key:").Secret());
        if (secret.Any(char.IsControl)) throw new ArgumentException("The key must not contain control characters.");
        Environment.SetEnvironmentVariable(variable, secret, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable(variable, secret);
        AnsiConsole.MarkupLine("[green]API key stored. Its value will not be displayed.[/]");
    }

    private static async Task ListModelsAsync(DemoConfigurationFile file, string target)
    {
        using var configuration = Effective(file);
        var settings = ConnectionOptions.FromConfiguration(configuration);
        var profile = target switch
        {
            "chat" => settings.LocalChat,
            "embeddings" when settings.EmbeddingProvider == ConnectionProvider.Ollama => settings.Embeddings,
            _ => throw new ArgumentException("Choose chat or an Ollama embeddings profile.")
        };
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        var endpoint = new Uri(new Uri(profile.Endpoint.TrimEnd('/') + "/"), "api/tags");
        using var result = await client.GetFromJsonAsync<JsonDocument>(endpoint) ?? throw new InvalidOperationException("No model catalogue returned.");
        var table = new Table().AddColumn("Installed Ollama model");
        foreach (var model in result.RootElement.GetProperty("models").EnumerateArray())
            table.AddRow(new Text(model.GetProperty("name").GetString() ?? ""));
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine("Listing does not install or download models.");
    }

    private static async Task CheckAsync(DemoConfigurationFile file, string target)
    {
        using var configuration = Effective(file);
        var settings = ConnectionOptions.FromConfiguration(configuration);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        AnsiConsole.WriteLine("Testing the saved profile with environment overrides. Unsaved menu edits are not used.");
        if (target == "chat")
        {
            using var chat = DemoConnection.CreateChatClient(settings);
            var response = await chat.GetResponseAsync(
                "Reply with the single word Ready.", cancellationToken: cancellation.Token);
            if (string.IsNullOrWhiteSpace(response.Text)) throw new InvalidOperationException("The chat response is empty.");
            AnsiConsole.MarkupLine("[green]Chat request succeeded.[/]");
        }
        else if (target == "embeddings")
        {
            using var embeddings = DemoConnection.CreateEmbeddingGenerator(settings);
            var result = await embeddings.GenerateAsync(["A configuration connectivity check."], cancellationToken: cancellation.Token);
            AnsiConsole.WriteLine("Embedding request succeeded. Dimensions: " + result[0].Vector.Length);
        }
        else throw new ArgumentException("Choose check chat or check embeddings.");
    }
}
