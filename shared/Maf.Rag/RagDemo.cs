using System.Globalization;
using System.Text.Json;
using Maf.DemoConnection;
using Microsoft.Extensions.Configuration;

namespace Maf.Rag;

/// <summary>Provides the shared SQLite RAG console, including reproducible audit and calibration commands.</summary>
public static class RagDemo
{
    /// <summary>Loads resources relative to the executable and uses an explicit absolute state root.</summary>
    public static async Task RunAsync(string[] args, string mode)
    {
        var configuration = new ConfigurationBuilder()
            .AddDemoConfiguration(args).Build();
        var settings = ConnectionOptions.FromConfiguration(configuration);
        string corpusPath = configuration["Rag:Corpus"] ?? Path.Combine(AppContext.BaseDirectory, "corpus.json");
        var corpus = Corpus.Load(Path.GetFullPath(corpusPath));
        var stateRoot = global::Maf.DemoConnection.DemoConfiguration.GetSetting("MAF_STATE_ROOT")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MafWorkshop", "rag");
        if (!Path.IsPathFullyQualified(stateRoot)) throw new ArgumentException("MAF_STATE_ROOT must be absolute.");
        string database = configuration["Rag:Database"] ?? Path.Combine(stateRoot, "vectors.db");
        var store = new SqliteVectorStore(database);
        using var index = new EmbeddingIndex(settings, corpus, store);
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            string scope = mode is "business" or "corpus" ? mode : "course";
            var date = configuration["Rag:AsOf"] is { } dateText
                ? DateOnly.ParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                : DateOnly.FromDateTime(DateTime.Today);
            Console.WriteLine($"SQLite vector RAG | mode={mode} | asOf={date:yyyy-MM-dd} | database={database}");
            string? action = configuration["Rag:Action"];
            if (action is not null) { await ExecuteAsync(action); return; }
            Console.WriteLine("Commands: rebuild | audit | compare | probe <question> | quit. Other input asks a question; EOF exits.");
            while (true)
            {
                Console.Write("Request > ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;
                if (input.StartsWith("probe ", StringComparison.Ordinal))
                {
                    Print(await index.RetrieveAsync(input[6..], date, scope, diagnostics: true, ct: stop.Token));
                    continue;
                }
                if (input is "rebuild" or "audit" or "compare") await ExecuteAsync(input);
                else await AskAsync(input);
            }

            async Task ExecuteAsync(string command)
            {
                switch (command)
                {
                    case "rebuild":
                        Console.WriteLine(JsonSerializer.Serialize(await index.RebuildAsync(stop.Token), Corpus.JsonOptions));
                        break;
                    case "audit":
                        foreach (var source in corpus.Documents)
                            Console.WriteLine($"{source.Id}: eligible={CorpusPolicy.Eligible(source, date, source.Scope)} "
                                + $"status={source.Status} canonical={source.Canonical} "
                                + $"valid={source.ValidFrom:yyyy-MM-dd}..{source.ValidUntil?.ToString("yyyy-MM-dd") ?? "open"}");
                        break;
                    case "probe":
                        Print(await index.RetrieveAsync(Question(), date, scope, diagnostics: true, ct: stop.Token));
                        break;
                    case "compare":
                        var comparisonQuestion = configuration["Rag:Question"] ?? (scope == "business"
                            ? "Which quality control procedure applies to GX-900 gears after heat treatment?"
                            : scope == "corpus" ? "Can I get train ticket reimbursement without a receipt?"
                            : "Where can I export CSV in Reporting?");
                        Console.WriteLine("Raw vector ranking: diagnostic only; never sent to the answer service.");
                        Print(await index.RetrieveAsync(comparisonQuestion, date, scope, diagnostics: true, ct: stop.Token, rawCorpus: true));
                        Console.WriteLine("Governed hybrid context:");
                        Print(await index.RetrieveAsync(comparisonQuestion, date, scope, ct: stop.Token));
                        break;
                    case "answer": await AskAsync(Question()); break;
                    case "verify":
                        await RagAcceptance.RunAsync(settings, corpus,
                            configuration["Rag:Evidence"] ?? Path.Combine(stateRoot, "acceptance"), stop.Token);
                        break;
                    default: throw new ArgumentException("Unknown Rag:Action; use rebuild, audit, probe or answer.");
                }
            }

            string Question() => configuration["Rag:Question"] ?? throw new ArgumentException("Rag:Question is required.");

            async Task AskAsync(string question)
            {
                var retrieval = await index.RetrieveAsync(question, date, scope, ct: stop.Token);
                Print(retrieval);
                var answer = await RagAnswerService.AnswerAsync(settings, retrieval, mode, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(answer, Corpus.JsonOptions));
            }
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static void Print(Retrieval retrieval) => Console.WriteLine(JsonSerializer.Serialize(retrieval, Corpus.JsonOptions));
}
