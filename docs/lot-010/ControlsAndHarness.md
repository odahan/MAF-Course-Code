# Middleware, bounded execution and native Harness

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

The canonical implementation is `shared/Maf.Control`. `demos/Demo330_BoundedHarness` runs a real **Microsoft.Agents.AI.Harness 1.26.0 HarnessAgent**. EP005 (`DisqueRaye.AgentDemo`) and EP006 (`RbacToolFilteringDemo`) use the same controls with a native ChatClientAgent. EP011 demonstrates explicit local degradation. Historical originals and the Lot-001 archive remain preserved.

## Run the examples

From `BaseTechnique`, after `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`:

```powershell
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- complete
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- repeat
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- forbidden
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- budget
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- changing
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- deadline
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- incomplete
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- complete --simple
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- complete --live
# Archived acceptance recipe: tools/Verify-Lot010.ps1 -Live
```

Without `--live`, the transport is scripted: the fixture emits actual `FunctionCallContent` responses and MAF invokes the registered tools. This verifies orchestration and policy behavior without claiming to measure a model. With `--live`, the shared configuration selects Ollama by default. Common `--AI:...=...` options can explicitly select the API-key profile. No automatic cloud fallback occurs. Output identifies the transport, native agent kind, limits, granted capabilities, ordered middleware/tool/model events and terminal status. Exit codes: 0 completed, 2 controlled stop/incomplete/provider unavailable, 130 caller cancellation.

## Capability boundary and normalization

`ExecutionScope` is created once per request and disposed after the run. The host grants `ticket.read` and `review.finish`; the forbidden scenario grants only the latter. These educational grants are host fixtures, not authenticated roles. Production code must derive them from trusted identity and resource authorization.

`CreateTool<T>` binds the input to its declared .NET type before checking permissions and repetition. The raw body is private inside a closure. Both `AIFunction.InvokeAsync` and native tool execution enter this same boundary. Hiding a tool from a catalogue is useful for selection but is not authorization. The forbidden scenario deliberately leaves the tool visible to exercise enforcement at execution time. Native `.AsBuilder().Use(...)` middleware records entry and checks cancellation; the guarded function enforces permissions and budgets even outside that middleware. Do not publish unguarded delegates or put raw functions into `AdditionalTools`.

Repetition keys include the tool name and serialized typed input. Object properties sort ordinally, decimal values normalize scale, JSON and CLR values bind to the same type. String case, whitespace, array order and null remain meaningful. There is no arbitrary lowercasing or stringification of all inputs. Counts are reserved atomically before invoking the body, including unsuccessful attempts, preventing concurrent duplicates from entering twice. Keys and arguments stay in memory and are not logged. Tool names must uniquely identify one implementation and declared input type in a scope.

The scope is **one run**, not a durable identity. A new scope permits the same request again. This loop control does not replace Lot-009's stable operation key, durable decision and unique transactional SQLite effect. The sample tools are read-only; `finish_review` marks completion in host memory and performs no business write.

## Explicit limits and completion

| Control | Default | Enforced at |
|---|---:|---|
| Model transport calls | 6 | BudgetChatClient, including calls after tool results |
| Tool attempts | 4 | Guarded typed function, shared across tools |
| Identical calls per tool and typed input | 1 | Atomic reservation in ExecutionScope |
| Entire run deadline | 120 seconds | Linked cancellation source passed to transport and bodies |
| Model output | 800 tokens per call | ChatOptions; provider support still applies |
| Native Harness invocation iterations | model-call limit + 1 | MaximumIterationsPerRequest; explicit outer model budget stops first |

`budget` reduces model calls to one; `deadline` reduces the deadline to one second. `changing` emits distinct unknown ticket IDs: each returns NotFound, and the global tool budget stops the loop even though repetition detection never fires. The model counter bounds text-only iterations and applies to streaming as well as non-streaming transports. The supplied console uses non-streaming runs.

The task must read ticket 42 and successfully call `finish_review(42)`. A final assistant statement alone returns **Incomplete**. A policy violation cancels the run and returns **PermissionDenied**, **RepeatedCall**, **ToolBudget**, **ModelBudget** or **DeadlineExceeded**, with no assistant success text. Caller cancellation propagates as `OperationCanceledException`, even if it races with an internal stop. Internal provider cancellation without cancellation of the run is not relabeled as a deadline.

Cancellation is cooperative. A body or provider ignoring its token can continue until it returns; this is not an OS sandbox or a hard CPU/memory limit. Already completed effects cannot be rolled back by cancellation. Keep mutation authorization and persistent idempotence at the business boundary. The 800-token output setting is not a measured currency or total token budget.

## Harness composition and choice

The demo explicitly disables default todo tracking, agent modes, file memory, skills discovery, web search and standing auto-approval. No shared file store, shell, background agents or outer loop evaluators are configured. Native OpenTelemetry remains enabled. No compaction token window is configured, so automatic compaction is not enabled; the experimental `DisableCompaction` API is deliberately not used. The demo does not enable experimental file-access or looping extensions.

| Runtime | Use in this renovation |
|---|---|
| Simple agent | Same two guarded tools and limits; straightforward short interaction |
| Lot-008 workflow | Host-defined typed branches and graph output validation |
| Native Harness | Composed native runtime with capability defaults explicitly narrowed; optional facilities can be introduced deliberately |

The narrowed Harness does not claim planning, file access or autonomous delegation. Its purpose is to make the composition, tool boundary and stopping conditions inspectable before adding capabilities. The [official Harness documentation](https://learn.microsoft.com/en-us/agent-framework/concepts/harness) describes its composition; actual options were also checked in the installed 1.26.0 XML documentation.

## EP011: degradation and cancellation

```powershell
dotnet run --project demos/Demo350_GracefulDegradation -c Release --no-build
dotnet run --project demos/Demo350_GracefulDegradation -c Release --no-build -- --unavailable
dotnet run --project demos/Demo350_GracefulDegradation -c Release --no-build -- --invalid
dotnet run --project demos/Demo350_GracefulDegradation -c Release --no-build -- --timeout
dotnet run --project demos/Demo350_GracefulDegradation -c Release --no-build -- --live
```

The first four paths produce `Origine=Heuristic`, a cause and `Confiance=null`. Keyword matching is a limited fallback, not semantic inference or measured confidence. Model responses also have no calibrated score; a model-supplied number is discarded. The French property/method names remain for source compatibility, while application messages and XML documentation are English.

Polly allows one model attempt with a 60-second timeout, or a two-second simulated timeout. Only expected provider/timeout/output exceptions degrade; programming failures outside those categories propagate. Caller cancellation is checked before explicit fallback, after a model result and inside the filtered failure handler. It can never be absorbed by a general catch. The model client is owned and disposed per request and uses the common connection profiles. There is no retry or implicit switch to another provider.

## Exercise

Start with `repeat`, inspect the first actual invocation and the following `RepeatedCall` event, then compare `changing`: different typed arguments avoid repetition detection but still exhaust ToolCalls. Set Repetitions to 2 and ToolCalls to 3 in a local copy, predict the body count, and verify it. Then deny `ticket.read` and directly invoke the returned AIFunction: no body should run. Restore the defaults and compare `complete` with `--simple` and with `--live`.

EP003's untrusted-data instructions and EP012's precise tool descriptions remain complementary selection/content demonstrations. Their wording cannot grant a capability or prove an effect. This lot wires EP005/EP006 and the new Harness path to execution-time controls; it does not claim every historical lab now shares this scope or that all C14 paths are migrated. Lot-005 business authorization and Lot-009 durable idempotence remain independent guarantees.
