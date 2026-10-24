using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Maf.Observability;

/// <summary>Exports correlation and timing only; native tags, events, names and exception messages are excluded.</summary>
public sealed record SafeSpan(string Source, string Operation, string TraceId, string SpanId,
    string ParentSpanId, double DurationMs, string Status);

/// <summary>Configures native MAF, workflow and HTTP tracing with a deliberately restricted exporter.</summary>
public static class Telemetry
{
    /// <summary>Gets the host-owned source for fixed operation names.</summary>
    public static ActivitySource Source { get; } = new("Maf.Renovation");

    /// <summary>Builds a provider whose sole exporter drops all arbitrary payloads.</summary>
    public static TracerProvider CreateProvider(Action<SafeSpan> write) => Sdk.CreateTracerProviderBuilder()
        .AddSource("Maf.Renovation", "Maf.NativeAgent", "Microsoft.Agents.AI*", "Microsoft.Extensions.AI*")
        .AddHttpClientInstrumentation()
        .AddProcessor(new SimpleActivityExportProcessor(new SafeExporter(write))).Build();

    /// <summary>Adds the native MAF decorator with sensitive data explicitly disabled.</summary>
    public static AIAgent Instrument(AIAgent agent) => agent.AsBuilder()
        .UseOpenTelemetry(sourceName: "Maf.NativeAgent", configure: options => options.EnableSensitiveData = false).Build();

    /// <summary>Projects even hostile activities without retaining raw names, URLs, tags, events or exception details.</summary>
    public static SafeSpan Project(Activity activity) => new(
        activity.Source.Name == Source.Name ? "host" : activity.Source.Name.StartsWith("System.Net.Http", StringComparison.Ordinal)
            ? "http" : activity.Source.Name.StartsWith("Microsoft.Agents.AI.Workflows", StringComparison.Ordinal)
                ? "maf.workflow" : activity.Source.Name.StartsWith("Microsoft.Extensions.AI", StringComparison.Ordinal)
                    ? "maf.model" : "maf.agent",
        activity.Source.Name == Source.Name && activity.OperationName is "execution" or "tool" or "workflow" or "model"
            ? activity.OperationName : "operation",
        activity.TraceId.ToString(), activity.SpanId.ToString(), activity.ParentSpanId.ToString(),
        activity.Duration.TotalMilliseconds, activity.Status.ToString());

    /// <summary>Writes safe spans as JSON lines.</summary>
    public static void Print(SafeSpan span) => Console.WriteLine("Trace: " + JsonSerializer.Serialize(span));

    private sealed class SafeExporter(Action<SafeSpan> write) : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch) write(Project(activity));
            return ExportResult.Success;
        }
    }
}
