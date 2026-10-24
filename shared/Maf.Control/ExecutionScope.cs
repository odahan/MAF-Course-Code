using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Maf.Control;

/// <summary>Defines hard limits for one run, including all model and tool iterations.</summary>
public sealed record ExecutionLimits(int ModelCalls = 6, int ToolCalls = 4, int Repetitions = 1,
    int DeadlineSeconds = 120);

/// <summary>Records a policy decision without exposing arguments or model content.</summary>
public sealed record ControlEvent(string Kind, string Name, int Count);

/// <summary>Owns run-local permissions, typed repetition keys, budgets and cooperative cancellation.</summary>
public sealed class ExecutionScope : IDisposable
{
    private readonly object sync = new();
    private readonly HashSet<string> capabilities;
    private readonly Dictionary<string, int> repetitions = new(StringComparer.Ordinal);
    private readonly List<ControlEvent> events = [];
    private readonly CancellationToken caller;
    private readonly CancellationTokenSource stop;
    private int models;
    private int tools;

    /// <summary>Creates an isolated scope; never share it across requests or tenants.</summary>
    public ExecutionScope(IEnumerable<string> capabilities, ExecutionLimits? limits = null,
        CancellationToken cancellationToken = default)
    {
        Limits = limits ?? new();
        if (Limits.ModelCalls < 1 || Limits.ToolCalls < 1 || Limits.Repetitions < 1 || Limits.DeadlineSeconds < 1)
            throw new ArgumentOutOfRangeException(nameof(limits));
        this.capabilities = new(capabilities, StringComparer.Ordinal);
        caller = cancellationToken;
        stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        stop.CancelAfter(TimeSpan.FromSeconds(Limits.DeadlineSeconds));
    }

    /// <summary>Gets immutable run limits.</summary>
    public ExecutionLimits Limits { get; }
    /// <summary>Gets the token passed to model transports and tool bodies.</summary>
    public CancellationToken Token => stop.Token;
    /// <summary>Gets the first terminal policy reason, or null before a stop.</summary>
    public string? StopReason { get; private set; }
    /// <summary>Gets a thread-safe evidence snapshot.</summary>
    public ControlEvent[] Events { get { lock (sync) return events.ToArray(); } }

    /// <summary>Checks cancellation with caller cancellation taking precedence over policy outcomes.</summary>
    public void CheckCancellation()
    {
        caller.ThrowIfCancellationRequested();
        Token.ThrowIfCancellationRequested();
    }

    /// <summary>Preserves caller cancellation even when an internal deadline fired concurrently.</summary>
    public void ThrowIfCallerCanceled() => caller.ThrowIfCancellationRequested();

    /// <summary>Records native middleware entry independently of the guarded body decision.</summary>
    public void ObserveMiddleware(string name)
    {
        lock (sync) events.Add(new("Middleware", name, tools));
    }

    /// <summary>Counts every transport call, including calls made after a function result.</summary>
    public void BeforeModel()
    {
        lock (sync)
        {
            CheckCancellation();
            if (models >= Limits.ModelCalls) Halt("ModelBudget", "model", models);
            events.Add(new("ModelCall", "model", ++models));
        }
    }

    /// <summary>Creates a guarded typed tool. The raw body stays private to the factory closure.</summary>
    public AIFunction CreateTool<T>(string name, string capability,
        Func<T, CancellationToken, Task<string>> body)
    {
        return AIFunctionFactory.Create(async (T input, CancellationToken cancellationToken) =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(Token, cancellationToken);
            linked.Token.ThrowIfCancellationRequested();
            lock (sync)
            {
                CheckCancellation();
                if (!capabilities.Contains(capability)) Halt("PermissionDenied", name, tools);
                if (tools >= Limits.ToolCalls) Halt("ToolBudget", name, tools);
                var key = name + ":" + Canonical(JsonSerializer.SerializeToElement(input));
                var count = repetitions.GetValueOrDefault(key);
                if (count >= Limits.Repetitions) Halt("RepeatedCall", name, count);
                repetitions[key] = count + 1;
                events.Add(new("ToolCall", name, ++tools));
            }
            var result = await body(input, linked.Token);
            CheckCancellation();
            return result;
        }, name, "Run a capability-checked operation with a typed input.");
    }

    private void Halt(string reason, string name, int count)
    {
        StopReason ??= reason;
        events.Add(new(reason, name, count));
        stop.Cancel();
        throw new OperationCanceledException(reason, Token);
    }

    private static string Canonical(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", element.EnumerateObject()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => JsonSerializer.Serialize(p.Name) + ":" + Canonical(p.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", element.EnumerateArray().Select(Canonical)) + "]",
        JsonValueKind.Number when element.TryGetDecimal(out var number) =>
            number.ToString("G29", System.Globalization.CultureInfo.InvariantCulture),
        _ => element.GetRawText()
    };

    /// <summary>Releases the linked deadline source after all operations have finished.</summary>
    public void Dispose() => stop.Dispose();
}
