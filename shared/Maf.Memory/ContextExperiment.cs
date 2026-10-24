using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.AI;

namespace Maf.Memory;

/// <summary>Demonstrates native MAF compaction and the separate effect on persisted history.</summary>
public static class NativeCompaction
{
    /// <summary>Reduces each outgoing request, while the original session history remains available.</summary>
    public static IChatClient CreateRequestView(IChatClient chat)
    {
#pragma warning disable MAAI001
        return chat.AsBuilder().UseAIContextProviders(new CompactionProvider(
            new SlidingWindowCompactionStrategy(CompactionTriggers.TurnsExceed(3), minimumPreservedTurns: 3))).Build();
#pragma warning restore MAAI001
    }

    /// <summary>Reduces retained history after each completed exchange; serialization saves the reduced list.</summary>
    public static InMemoryChatHistoryProvider CreateBoundedHistory()
    {
#pragma warning disable MAAI001
        return new(new InMemoryChatHistoryProviderOptions
        {
            ChatReducer = new SlidingWindowCompactionStrategy(CompactionTriggers.TurnsExceed(3), minimumPreservedTurns: 3).AsChatReducer(),
            ReducerTriggerEvent = InMemoryChatHistoryProviderOptions.ChatReducerTriggerEvent.AfterMessageAdded
        });
#pragma warning restore MAAI001
    }
}

/// <summary>Runs reproducible context policies over the same chronological user assertions.</summary>
public static class ContextExperiment
{
    /// <summary>Compares full history, recent window, generated summary, and controlled user facts.</summary>
    public static async Task RunAsync(IChatClient chat, CancellationToken cancellationToken = default)
    {
        string[] turns =
        [
            "customer=ACME-42; module=Reporting",
            "database=PostgreSQL; environment=production",
            "database=SQL Server 2022; module=Billing",
            "constraint=Do not restart the nightly batch",
            "version=9.4.17; log=BILLING-REPLAY-17",
            "Thank you. The weather is pleasant today.",
            "We are collecting more information from support.",
            "Please prepare a concise handover for the next team."
        ];
        string question = "Which module and database are affected, and which action is prohibited? Mention missing information.";
        var agent = chat.AsAIAgent(name: "ContextComparison", instructions: MemoryInstructions.Answer);
        var memory = new WorkingMemory();
        var extraction = new MemoryExtraction(chat.AsAIAgent(name: "FactExtractor", instructions: MemoryInstructions.Extractor), memory);
        foreach (string turn in turns)
            Console.WriteLine(await extraction.UpdateAsync(turn, cancellationToken));

        string full = string.Join('\n', turns);
        var summarizer = chat.AsAIAgent(name: "NarrativeSummary", instructions:
            "Summarize these chronological user assertions in English in at most 700 characters. Preserve corrections and prohibitions. Do not invent facts.");
        var summaryResponse = await summarizer.RunAsync(full, cancellationToken: cancellationToken);
        MemoryDemo.PrintUsage(summaryResponse);
        string summary = summaryResponse.Text;
        if (string.IsNullOrWhiteSpace(summary) || summary.Length > 700)
            throw new InvalidDataException("Summary rejected: empty or longer than 700 characters. No fallback.");

        (string Name, string Context)[] policies =
        [
            ("full", full),
            ("window-last-3", string.Join('\n', turns.TakeLast(3))),
            ("generated-summary-unverified", summary),
            ("bounded-user-facts", memory.ToPromptBlock())
        ];
        foreach (var policy in policies)
        {
            Console.WriteLine($"Policy={policy.Name}; context characters={policy.Context.Length}; character/4 estimate={(policy.Context.Length + 3) / 4} tokens (not provider usage).");
            var response = await agent.RunAsync(policy.Context + "\nQuestion: " + question, cancellationToken: cancellationToken);
            Console.WriteLine(response.Text);
            MemoryDemo.PrintUsage(response);
        }

        using var compacted = NativeCompaction.CreateRequestView(chat);
        var compactAgent = compacted.AsAIAgent(name: "NativeWindow", instructions: MemoryInstructions.Answer);
        var session = await compactAgent.CreateSessionAsync(cancellationToken);
        foreach (string turn in turns)
            await compactAgent.RunAsync(turn, session, cancellationToken: cancellationToken);
        var compactResponse = await compactAgent.RunAsync(question, session, cancellationToken: cancellationToken);
        Console.WriteLine("Native MAF request compaction: last three turns, source session history preserved. Older facts may be absent.");
        Console.WriteLine(compactResponse.Text);
        MemoryDemo.PrintUsage(compactResponse);
        Console.WriteLine($"Source serialized session characters: {(await compactAgent.SerializeSessionAsync(session, cancellationToken: cancellationToken)).GetRawText().Length}. This is storage size, not token usage.");
    }
}
