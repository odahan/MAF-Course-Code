using Microsoft.Extensions.AI;

namespace Maf.Control;

/// <summary>Provides two private read-only operations and a host-validated completion condition.</summary>
public sealed class ReviewTask
{
    private bool read;
    /// <summary>Gets whether both expected steps completed successfully.</summary>
    public bool Completed { get; private set; }
    /// <summary>Gets how many actual reads reached the body.</summary>
    public int Reads { get; private set; }

    /// <summary>Publishes guarded capabilities; no writer or raw operation is exposed.</summary>
    public IList<AITool> CreateTools(ExecutionScope scope) =>
    [
        scope.CreateTool<int>("read_ticket", "ticket.read", (id, token) =>
        {
            token.ThrowIfCancellationRequested();
            if (id != 42) return Task.FromResult("NotFound: only ticket 42 is in scope.");
            read = true;
            Reads++;
            return Task.FromResult("Ticket 42: Login returns HTTP 401 for a known account. No changes requested.");
        }),
        scope.CreateTool<int>("finish_review", "review.finish", (id, token) =>
        {
            token.ThrowIfCancellationRequested();
            if (!read || id != 42) throw new InvalidOperationException("Read ticket 42 before completing review.");
            Completed = true;
            return Task.FromResult("Review completed. No business write performed.");
        })
    ];
}
