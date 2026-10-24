# Using the renovated MAF base

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

This is a technical teaching base with a complete path from deterministic code to a documented, approved action. There is no imposed course duration. `J1`/`J2`, Video2–5 and the episode names identify provenance, not a timetable. Start here; older slides, scripts, exercises and ZIP files remain historical references.

First use [00-ConfigurateurDemo](../../demos/00-ConfigurateurDemo/README.md) for central configuration. The current solution contains 79 projects, including this setup utility added after the 78-project Lot-016 baseline. New demo processes load the shared settings directly; saved profile changes do not require rebuilding. Environment and command-line overrides retain priority.

## Prerequisites and first run

Use Windows with PowerShell 7 and the .NET SDK pinned in `global.json` (10.0.402). Windows is required to build the complete solution including WPF. NuGet access is needed for an initial restore. No vector server, Docker, cloud account or model is needed for the deterministic acceptance below. Review `AVIS-LEGAL.txt` and the source licences before redistribution.

Open PowerShell in BaseTechnique and follow the locked restore, build and test commands in the [root README](../../README.md). The retained test projects cover deterministic application behavior. Historical separate-process acceptance reports are preserved in ReserveBaseTechnique; they are dated evidence, not current launch prerequisites.

For an offline machine with an already populated package cache, set `NUGET_PACKAGES` to its absolute directory and run `dotnet restore BaseTechnique.slnx -m:1 --locked-mode --ignore-failed-sources -p:NuGetAudit=false`. This explicitly disables the online vulnerability audit and tolerates unavailable feeds; missing packages still fail restoration. The distribution does not contain a package cache. An offline cached restore is not an online security audit. The archive contains sources, not compiled programs or personal state.

## Connection and storage

For live scenarios install/run Ollama separately and provision the configured models (`granite4:3b` and `embeddinggemma:300m` are the distributed defaults). Use the central configurator to validate your settings, then follow the [connection guide](../lot-003/Connection.md). Chat and embeddings have independent providers/endpoints/models. A chat profile change does not rebuild or switch the embedding index automatically. API compatible mode requires an explicitly configured endpoint, model and environment-held key. No automatic cloud fallback or ChatGPT account connection is implemented. Qualify each actual provider/model/protocol before relying on its capabilities.

Configuration is copied as `ai-settings.json` beside each built executable. Assets resolve from the executable directory; absolute caller-owned directories hold acceptance state. The [memory guide](../lot-006/SessionsAndMemory.md) specifies the separate lifecycles for business data, session history and facts. `clear`/`forget` operations are not interchangeable with deleting all business data. Never reset production or historical data to run an exercise.

## Progression and observable transitions

Run console examples with `dotnet run --project <project.csproj> -c Release --no-build`, from the extracted root. Paths below are relative to that root. The linked guides provide arguments, expected outcomes and failure cases.

| Step | Problem, entry point and prerequisite | Observable result / next step |
|---|---|---|
| P00 | Decide whether code is enough: `demos/Demo010_CodeOrAgent`; C# basics | Deterministic route versus optional agent. C# orchestration is distinct from a MAF graph. Continue with connection setup. |
| P01 | `demos/Demo040_FirstAgent`; [connection](../lot-003/Connection.md), capabilities (unpublished local archive) | First native agent response and explicitly identified profile. Proceed to contracts. |
| P02–04 | StructuredDraft → ToolsCatalog → SqliteBacklog_Extended → PipelineComplete in [the catalogue](../DemoCatalog.md); [guide](../lot-005/ContractsAndTools.md) | Validate fields, observe real tools, approve/refuse and read SQLite. In-process approval is not durable idempotence. |
| P05 | StatePersistence and SharedMemory in [the catalogue](../DemoCatalog.md); [guide](../lot-006/SessionsAndMemory.md); P01–03 | Restart preserves history; corrected facts have provenance; forgetting removes the defined stores and active context. |
| P06 | `demos/Demo180_VectorIngestion`, Lab03_RagAgent and Lab03B_RagProvider; [guide](../lot-007/CorpusAndRetrieval.md); P02–03 | Rebuilt dense vectors in Microsoft.Data.Sqlite, exact C# cosine, eligibility before ranking, validated quotations or refusal. |
| P07 | `demos/Demo230_TypedWorkflow`; [guide](../lot-008/Workflows.md); P02–04 | Native graph branches, typed messages and actual agent step. A JSON plan remains data until the host executes it. |
| P08 | `demos/Demo300_ExternalRequest`; [guide](../lot-009/DurableApproval.md); P07 | Pending request survives process exit; same decision replay returns the same business row. |
| P09 | `demos/Demo330_BoundedHarness`; [guide](../lot-010/ControlsAndHarness.md); P03/P07 | Real bounded loop, host permissions, cancellation and explicit degradation. |
| P10 | `demos/Demo400_LocalMcp`; [guide](../lot-011/LocalMcp.md); P03 | Precompiled stdio server, selected tools and actual Found/NotFound results. Server catalogue is not authorization. |
| P11 | `demos/Demo410_StreamingInterfaces`; [guide](../lot-012/Streaming.md); P05/P10 | Shared backend, session isolation, exact stream reconstruction, cancellation and WPF MVVM Toolkit. Graphical interactive acceptance remains unperformed. |
| P12 | Lab07_OpenTelemetry and AuditTrailAgentique; [guide](../lot-013/Observability.md); P05/P08/P09 | Correlated safe traces and separately verified business journal; nullable usage is distinct from estimates and costs. |
| P13 | `demos/Demo460_CandidateEvaluation`; [guide](../lot-014/Evaluation.md); earlier guarantees | Isolated candidate inputs, versioned references and strict gates. The recorded default granite4:3b candidate/judge campaign is rejected. |
| P14 | `demos/Demo470_DocumentedApproval`; [scenario](Scenario.md); P04/P06/P08/P12 | A validated citation becomes the exact draft awaiting durable approval; restart, reject, approve and replay are observable. |
| P15 | `demos/Demo480_ModelRouting`; observations (unpublished local archive); P09/P13 | Optional routing experiment including auxiliary requests and direct baseline; no general saving is claimed. |

Choose [exercises](Exercises.md) after the matching prerequisite. Use the [audit closure matrix](AuditClosure.md) to trace claims to tests and dated acceptance. The [extension map](Extensions.md) identifies the next design choices.

## Guarantees and practical limits

Host validation establishes admissibility, not the truth of every model conclusion. A structured result can be invalid business data. A model can produce content and structured tool calls; the host controls execution. Retrieval may be explicit context, a provider or a tool. SQLite shares an ADO.NET style with SQL Server but has different SQL, concurrency and operational properties. An approval is a decision, not an accuracy guarantee. Session history, controlled facts, request arguments and injected services have distinct lifecycles.

The integrated scenario is a local console host. The separate Video5 application demonstrates streaming/MCP and sessions; it does not expose the integrated approval command over HTTP. Separate process traces correlate through the persisted operation ID; this is not a distributed cross-restart trace or an atomic journal across modules. File locking is single-host coordination. Authentication, multi-tenant authorization, remote hosting, encryption, backup recovery and deployment monitoring require application work. EP018 is a review checklist, never evidence that those controls exist.

The measured default judge failure remains a delivery reservation. The application tests and successful live examples do not promote that model to qualified status. No new API-wide qualification, independent human reference sign-off or graphical UI acceptance is inferred. Publication, support format, course duration and editorial schedules remain future decisions.

## Historical material

Previous distribution archives, acceptance tools and reports are preserved in ReserveBaseTechnique (unpublished local archive). They describe earlier snapshots. The cleaned source tree can be built directly with the commands in the root README.
