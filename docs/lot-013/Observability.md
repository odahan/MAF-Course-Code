# Observability, business evidence and consumption

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

The shared `Maf.Observability` module is used by Lab07, EP008 (`AuditTrailAgentique`), EP015 and the optional EP016 routing experiment. Connections still come from `Maf.DemoConnection`: Ollama by default, or an explicitly configured OpenAI-compatible API key profile. No collector, Docker or cloud account is required. EP016 remains an independent Ollama experiment.

## Run the examples

From `BaseTechnique` after the Release build:

```powershell
dotnet run --project demos/Demo420_SafeTelemetry -c Release --no-build -- --db E:/temp/maf-journal.db
dotnet run --project demos/Demo430_BusinessAudit -c Release --no-build -- --db E:/temp/maf-journal.db --deterministic
dotnet run --project demos/Demo430_BusinessAudit -c Release --no-build -- --db E:/temp/maf-journal.db --deterministic --fail
dotnet run --project demos/Demo430_BusinessAudit -c Release --no-build -- --db E:/temp/maf-journal.db --read EXECUTION_ID
dotnet run --project demos/Demo430_BusinessAudit -c Release --no-build -- --db E:/temp/maf-journal.db --erase EXECUTION_ID
# Archived acceptance recipe: tools/Verify-Lot013.ps1 -Live
```

The default database is `maf-observability.db` in the system temporary directory. Use an explicit path for durable teaching evidence. The new minimal journal schema is separate from historical EP008 databases; no original database is migrated or overwritten. EOF menus and generated fallback decisions have been removed from the renovated EP008 entry point.

## Three different records

| Record | Purpose | What it cannot prove |
|---|---|---|
| MAF workflow event | Show executor progress and graph completion | Persistence, business effect, HTTP success or usage |
| OpenTelemetry span | Relate host execution, native agent, model request, native workflow, tool and HTTP timing/status | A durable business transaction or complete unsampled history |
| SQLite journal | Reopen minimal host events and verify a committed local action | Remote side effects, immutable evidence or global exactly-once delivery |

The live scenario calls a native MAF agent with the `get_review_policy` read tool, checks that the tool actually ran, executes the existing typed backlog workflow with its native reviewer, validates the output, and commits a local `ReviewRequested` row. The deterministic variant runs the host workflow/transaction without model inference. `--fail` injects failure immediately before the business commit. No generated answer is used as write evidence.

`RequestReceived`, `Intention` and `Acknowledged` are metadata only. `ActionCommitted` is emitted solely by `CommitReview`, inside the **same SQLite transaction** as the `RecordedDecisions` row. The execution GUID is a unique business key: repeating that commit returns the existing row and creates no second event. `HasAction` reads the actual business table. An execution may fail after a previous action committed; failure never implies rollback of an earlier transaction. The durable approval workflow from Lot-009 remains the example for an external human decision and checkpoint replay; this lot does not replace its approval policy.

## Native instrumentation and privacy policy

`Telemetry.Instrument` uses MAF's native `UseOpenTelemetry` decorator with the explicit `Maf.NativeAgent` source and `EnableSensitiveData=false`. Workflows use native `WithOpenTelemetry`; outgoing requests use `AddHttpClientInstrumentation`. The host adds fixed execution/model/tool/workflow spans. The exporter preserves trace ID, span ID, parent ID, duration, fixed source category, fixed operation category and status. A provider observes the complete local example; sampled/remote production configurations require separate qualification.

The custom OpenTelemetry exporter is the only exporter installed here. It projects spans into a closed `SafeSpan` shape before output. It drops arbitrary activity/display names, tags, baggage, events, stack traces, exception messages, HTTP URLs and resource attributes. Native instrumentation may internally create exception events; they are never serialized by this exporter. Do not replace it with a raw ConsoleExporter or an unfiltered OTLP exporter. Additional in-process listeners and external application logging are outside this guarantee.

The journal accepts a host-generated GUID, UTC timestamp, current trace ID, closed event kind and optional numeric action ID. There is no free-form payload, user identifier, prompt, answer, tool argument/result, API key or exception text column. Failures are classified as failed or cancelled, with no arbitrary exception message. The demo reports safe errors and never substitutes a generated fallback action. The older EP008 `containsRawSecret=false` assertion has been replaced by a restricted data shape and tests.

On each new Lab07/EP008 execution, metadata older than **30 days** is purged. Read-only inspection does not trigger retention cleanup. `--erase GUID` deletes that execution's metadata. `PurgeBefore` and `Erase` never undo business actions; business-row retention is a separate policy. Traces are written to the caller's output with no built-in file persistence; whoever redirects them owns file access and retention. Use OS permissions for the database and logs. SQLite deletion is logical deletion, not forensic erasure of free pages, filesystem snapshots or backups. The journal is not signed, append-only or tamper-evident. Historical material and acceptance artifacts are preserved separately.

## Consumption and comparisons

`ObservedChatClient` sits below the native agent tool loop. It reports each real transport request once, so a second tool-loop turn is not hidden by a single agent-response aggregate. Streaming usage deltas are aggregated once at completion; failed, cancelled or abandoned streams report unknown completion usage. There are no retries in this observer. Unreported tokens or work done remotely after a disconnect remain unknown.

`Consumption` keeps nullable provider input, output and total counters. `DerivedTotal` is available only when both input and output are known; it does not overwrite the provider total. Character/4 is a labelled context-only heuristic, not a tokenizer or a substitute for provider usage. `UsageLedger` displays request coverage, known subtotals and nullable complete totals. No requests means no measured total, not a free model invocation.

No price is configured by default. An optional `TokenRate` carries nonnegative prices per million, currency and a version/source assumption supplied by the caller for the selected model. `EstimatedCost` requires both actual counters; it is an estimate under that assumption, not a provider invoice. Cached/reasoning token categories, GPU time, electricity and hosting are not priced by this teaching calculator. Do not use it as a multi-provider billing engine.

EP015 reports both raw and selected fixture contexts, then full history, a recent window, an unverified generated summary, controlled facts and native compaction. Every extraction, summary and compaction request is observed. Character counts and provider counters measure different things. Auxiliary calls and lost factual coverage must be included in any later quality/cost evaluation. Console business answers are deliberately displayed to the learner; redirecting an entire interactive transcript is separate from the safe trace export.

EP016 uses fresh sessions and identical final prompts for each paired request: triage plus routed execution, then a direct hard-model baseline. It reports each stage's model, duration and usage, and separate ledger totals including triage overhead. Sensitive requests use `--sensitive` and escalate without inference. Unrecognized triage output fails explicitly. The fixed HTTP-503 case is forced to the hard route by host policy. The classifier is a teaching fixture, not a security boundary or a qualified production router.

Set `MAF_ROUTE_TRIAGE`, `MAF_ROUTE_EASY` and `MAF_ROUTE_HARD` to already installed model names to compare distinct profiles. Defaults reuse the common local model so the ordinary course setup needs no extra model. Identical profiles demonstrate mechanics only. Fixed execution order, warmup, cache effects, different tokenizers and unevaluated answer quality prevent a general savings claim. See the acceptance report for the profiles actually exercised. Lot-014 will address reproducible quality/regression evaluation.
