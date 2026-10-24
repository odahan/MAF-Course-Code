using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace AgentCiCd.Tests;

/// <summary>
/// Faux client LLM déterministe.
/// Il implémente IChatClient afin de rester compatible avec Microsoft Agent Framework.
/// Aucun appel réseau, aucun coût, aucune variation de réponse.
/// </summary>
public sealed class DeterministicChatClient(string responseText) : IChatClient
{
    public ChatClientMetadata Metadata { get; } =
        new(nameof(DeterministicChatClient), new Uri("memory://tests"), "fake-deterministic-model");

    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];
    public string LastPromptText { get; private set; } = string.Empty;

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // On mémorise les messages afin que le test puisse vérifier le prompt transmis par l'agent.
        LastMessages = chatMessages.ToList();
        LastPromptText = string.Join(Environment.NewLine, LastMessages.Select(message => message.ToString()));

        // On renvoie toujours exactement le même contenu : c'est le principe du double de test.
        return Task.FromResult(new ChatResponse(new ChatMessage(
            ChatRole.Assistant,
            responseText)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // La démo n'utilise pas le streaming, mais l'interface IChatClient l'exige.
        LastMessages = chatMessages.ToList();
        LastPromptText = string.Join(Environment.NewLine, LastMessages.Select(message => message.ToString()));
        await Task.Yield();
        yield return new ChatResponseUpdate(ChatRole.Assistant, responseText);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;

    public TService? GetService<TService>(object? key = null)
        where TService : class => this as TService;

    void IDisposable.Dispose()
    {
        // Rien à libérer : ce faux client ne possède aucune ressource externe.
    }
}
