using Maf.Backlog;
using Microsoft.Agents.AI.Workflows;

namespace Maf.Durable;

/// <summary>Builds a versioned graph with stable executor and request-port identifiers.</summary>
public static class ApprovalWorkflow
{
    /// <summary>Gets the graph contract version required to reconstruct stored checkpoints.</summary>
    public const string Version = "backlog-approval-v1";

    /// <summary>Creates an actionable example shared by all three Video3 hosts.</summary>
    public static ApprovalTicket Example(string operationId) => new(operationId,
        new BacklogDraft("Regional pilot", "Feature", "medium", "API",
            "Deploy the approved pilot to ten regional agencies.", []));

    /// <summary>Constructs the same graph for initial execution and restoration.</summary>
    public static Workflow Build(ApprovalStore store, bool failAfterWrite = false)
    {
        var prepare = new Prepare(store);
        var port = RequestPort.Create<ApprovalTicket, ApprovalDecision>("BusinessApprovalV1");
        var apply = new Apply(store, failAfterWrite);
        return new WorkflowBuilder(prepare).AddEdge(prepare, port).AddEdge(port, apply)
            .WithOutputFrom(apply).Build();
    }

    private sealed class Prepare(ApprovalStore store) : Executor<ApprovalTicket, ApprovalTicket>("PrepareApprovalV1")
    {
        public override ValueTask<ApprovalTicket> HandleAsync(ApprovalTicket message, IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            store.Register(message);
            return ValueTask.FromResult(message);
        }
    }

    private sealed class Apply(ApprovalStore store, bool failAfterWrite)
        : Executor<ApprovalDecision, ApprovalState>("ApplyApprovalV1")
    {
        public override ValueTask<ApprovalState> HandleAsync(ApprovalDecision message, IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var result = store.Apply(message);
            if (failAfterWrite) throw new InvalidOperationException("Injected failure after the SQLite commit.");
            return ValueTask.FromResult(result);
        }
    }
}
