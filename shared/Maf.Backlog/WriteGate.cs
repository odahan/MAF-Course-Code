using System.ComponentModel;

namespace Maf.Backlog;

/// <summary>Binds one host-approved immutable draft to a one-shot invocation capability.</summary>
public sealed class WriteGate
{
    private readonly BacklogStore store;
    private readonly BacklogDraft draft;
    private readonly bool canWrite;
    private readonly string proposalId = Guid.NewGuid().ToString("N");
    private bool approved;
    private bool consumed;
    private readonly object sync = new();
    private readonly List<ToolOutcome> observations = [];

    /// <summary>Copies untrusted mutable arrays before they cross the review boundary.</summary>
    public WriteGate(BacklogStore store, BacklogDraft draft, bool canWrite)
    {
        this.store = store;
        this.draft = draft with { ClarifyingQuestions = draft.ClarifyingQuestions?.ToArray()! };
        this.canWrite = canWrite;
    }

    /// <summary>Gets a host-generated identifier, never an authorization supplied by the model.</summary>
    public string ProposalId => proposalId;

    /// <summary>Gets a snapshot of actual tool invocations, including refusals and storage errors.</summary>
    public IReadOnlyList<ToolOutcome> Observed { get { lock (sync) return observations.ToArray(); } }

    /// <summary>Approves only the expected pending call after review of this gate's fixed draft.</summary>
    public bool Approve(string toolName, string? requestedId, bool accepted)
    {
        lock (sync)
        {
            if (consumed || !accepted || toolName != "create_backlog_item" || requestedId != proposalId
                || !canWrite || draft.Validate().Length != 0) return false;
            approved = true;
            return true;
        }
    }

    /// <summary>Writes the fixed reviewed draft once; rejects unknown, denied or reused proposals.</summary>
    [Description("Creates the host-reviewed backlog draft in SQLite once. Requires a matching proposalId, host write permission and human approval. Returns Created, Invalid, Refused or Error with an optional stored item ID.")]
    public ToolOutcome Create([Description("Exact proposal ID issued by the host for this reviewed draft.")] string proposalId)
    {
        lock (sync)
        {
            ToolOutcome result;
            if (draft.Validate().Length != 0) result = new("Invalid", null, "Business contract failed.");
            else if (!canWrite || !approved || consumed || proposalId != this.proposalId)
                result = new("Refused", null, "Permission, approval or unused matching proposal is missing.");
            else
            {
                consumed = true;
                approved = false;
                try { result = new("Created", store.Insert(draft), "SQLite write observed by the host."); }
                catch (Microsoft.Data.Sqlite.SqliteException) { result = new("Error", null, "SQLite write failed; inspect storage before retrying."); }
                catch (IOException) { result = new("Error", null, "Storage is unavailable."); }
                catch (UnauthorizedAccessException) { result = new("Error", null, "Storage access was denied."); }
            }
            observations.Add(result);
            return result;
        }
    }
}
