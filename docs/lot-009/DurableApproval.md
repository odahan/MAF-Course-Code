# External decisions, checkpoints and idempotent effects

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

The three renovated Video3 copies use an actionable `BacklogDraft` from Lot-005. The historical offer/budget strings have become a typed approval request for a regional pilot backlog item. This makes the result an inspectable business write. Historical sources, films and reference documents remain unchanged.

## Three progressively stronger examples

| Entry point | Engine state | Response | Scope |
|---|---|---|---|
| Video3.ExternalRequestPause | Live process | Host-supplied CLI decision | Real typed request/response; immediate simulated external party |
| Video3.CheckpointResume | `CreateInMemory()` | Host-supplied CLI decision after graph reconstruction | Same-process restoration only; no process durability |
| Video3.ApprovalLikeRequest | Native MAF JSON files plus SQLite | Separate `resume` command | Waiting state survives process exit; repeated response creates at most one business row |

No model, Ollama server, API credential, embedding or container is needed. These examples isolate workflow mechanics. Run from `BaseTechnique` after `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`:

```powershell
dotnet run --project 'demos/Demo300_ExternalRequest' -c Release --no-build -- approve
dotnet run --project 'demos/Demo310_CheckpointResume' -c Release --no-build -- reject

$demo = 'demos/Demo320_DurableApproval'
dotnet run --project $demo -c Release --no-build -- start ./artifacts/approval-example
# The first process has now ended. Keep this directory intact.
dotnet run --project $demo -c Release --no-build -- status ./artifacts/approval-example
dotnet run --project $demo -c Release --no-build -- resume ./artifacts/approval-example approve
dotnet run --project $demo -c Release --no-build -- resume ./artifacts/approval-example approve
dotnet run --project $demo -c Release --no-build -- status ./artifacts/approval-example
```

Use a fresh directory for another operation. `reject` persists a refusal without creating an item. `maybe`, an empty comment, a mismatched operation/request or a decision conflicting with the first recorded response fails. The CLI default comment is stable; custom comments must also match exactly on replay. An identical response is accepted, while an edited response requires a new operation. The tool returns a nonzero exit code on validation, storage or engine failure.

## The exact checkpoint boundary

The graph is `PrepareApprovalV1 -> BusinessApprovalV1 -> ApplyApprovalV1`. Preparation validates and registers the immutable draft, then sends an `ApprovalTicket` to a typed `RequestPort<ApprovalTicket, ApprovalDecision>`.

A superstep is an engine execution/synchronization cycle over queued messages and their recipients. It is not a freely chosen business subgroup. `CaptureWaitingAsync` first observes `RequestInfoEvent`, then retains the checkpoint on the following `SuperStepCompletedEvent`. An earlier checkpoint is deliberately insufficient. The native checkpoint contains the pending request, messages and engine state. The receipt is published only after that checkpoint exists; it is written through a temporary file and rename.

`waiting.json` retains the graph version, session ID, checkpoint ID, operation ID and engine request ID. The host reconstructs the same graph with fixed executor/port IDs and the same message contracts, opens a new `FileSystemJsonCheckpointStore`, and calls `ResumeStreamingAsync`. The pending request is re-emitted with the same request ID. Acceptance checks that `PrepareApprovalV1` is not invoked on resume and that `ApplyApprovalV1` is invoked. No draft or pending request is reconstructed from a console string.

The waiting receipt is intentionally retained after completion to exercise stale-checkpoint replay. Later checkpoints can exist in the native store, but are not used to bypass replay testing. Keep the receipt, checkpoint directory and SQLite database together. Changing graph identities, contracts or version needs a migration strategy; this example rejects an unknown graph version.

## Decision and effect guarantees

`OperationId` is generated once at start. It is the idempotence key for one business operation, not an invocation counter or an engine-generated transient response ID. `Approvals.OperationId` binds it to the serialized draft. A different draft cannot reuse the key. The host compares the engine request ID and operation ID before accepting the response.

The first valid decision is recorded in SQLite before `SendResponseAsync`. This separate durable step prevents a changed response from being accepted after a crash. A recorded approval without an effect is recoverable: replay sends that same decision again. Refusal never enters the insert branch. The effect executor revalidates the decision against the persisted binding.

`Apply` uses a SQLite transaction to insert into `ApprovedBacklogItems` and set the approval to `Completed`. A unique constraint on `ApprovedBacklogItems.OperationId` prevents duplicate effects, including concurrent calls. The business row contains the draft, creation time and stable item ID. Repetition returns the existing item. `status` joins the real item and returns its stored content as `StoredItem`, independently of the workflow output.

The approval decision is a process-level business decision. It does not use or replace MAF function-tool approval from Lot-005. This local write is protected explicitly by the decision store and transaction.

| Interruption point | Persisted state | Recovery |
|---|---|---|
| After waiting receipt, before response | Waiting; no item | Resume and provide a valid decision |
| After decision, before effect | Approved or Rejected; no item | Send the identical decision again |
| During business transaction | Insert and completion roll back together | Retry the same operation |
| After commit, before engine completion | Completed and one item | Replay the waiting checkpoint; same item ID |

To exercise the last window, resume a fresh waiting operation with `approve 'External business decision.' --fail-after-write`. The executor throws after the commit and the process exits with code 1. `status` still reads the completed item. Resume without the flag using the identical response: the workflow finishes with the same item.

## Verification and exercise

`./tools/Verify-Lot009.ps1` runs the three hosts and stores separate-process logs, native checkpoint files, receipts and actual SQLite databases in `docs/lot-009/runs/<id>`. It checks approve/reject/invalid/replay/conflicting decision and the injected post-commit failure, with stdin closed. `smoke-result.json` records process IDs and commands. The deterministic test project additionally checks SQL row counts, operation rebinding, mismatched response correlation, concurrent duplicate calls and rollback using a failing SQLite trigger.

Exercise: start a fresh directory; record its request ID and draft; submit an invalid decision; inspect `status`; reject it; replay rejection; attempt approval. Expected result: the original request identity is retained, invalid input leaves it waiting, rejection remains final and `ApprovedBacklogItems` contains zero rows for that operation. Repeat with approval and a post-commit failure; expect one row with the same ID after every replay. These paths are implemented in the acceptance script and tests.

## Limits

This is a local, single-host educational service. A file lease excludes concurrent CLI hosts for the same directory because the native file checkpoint store requires process-exclusive access. SQLite protects concurrent calls to the business store, but this is not a distributed workflow scheduler.

Checkpoint files, receipt and database are not one transaction. Process durability is demonstrated after a successful `start` and during recovery after a committed effect. A crash during initial checkpoint/index publication, loss/corruption of files, power failure and remote filesystem semantics are not qualified. If start fails before publishing a receipt, preserve that directory for inspection and use a fresh one; automatic orphan discovery is not provided.

The uniqueness guarantee covers the local SQLite effect only. A remote payment, email or ticket service would need its own durable idempotency key or an outbox/receiver contract. There is no claim of exactly-once execution of every executor. Authentication, reviewer authorization, expiration, amendment workflows, encryption and retention are outside this lot; filesystem/database access is trusted in this demo.
