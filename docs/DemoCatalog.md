# Demo catalogue

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Start with [00-ConfigurateurDemo](../demos/00-ConfigurateurDemo/README.md), the central setup utility added after Lot-016. It does not renumber the 49 learning demos.

The structured catalogue is [DemoCatalog.json](DemoCatalog.json). Identifiers are stable; chapter numbers belong to the [course map](CourseMap.md).

| Demo | Step | Prerequisites | Mode | Objective |
|---|---|---|---|---|
| [Demo010 — Code Or Agent](../demos/Demo010_CodeOrAgent/README.md) | P00 | C# basics | deterministic | Compare deterministic routing, C# orchestration and optional open-ended reasoning. |
| [Demo020 — Connection Profiles](../demos/Demo020_ConnectionProfiles/README.md) | P01 | Demo010 | real | Select and diagnose independent chat and embedding connections, from console or HTTP. |
| [Demo030 — Agent Capabilities](../demos/Demo030_AgentCapabilities/README.md) | P01 | Demo020 | real | Qualify the actual provider capabilities before depending on them. |
| [Demo040 — First Agent](../demos/Demo040_FirstAgent/README.md) | P01 | Demo030 | real | Send unstructured intake text to a native MAF agent. |
| [Demo050 — Structured Business Contract](../demos/Demo050_StructuredBusinessContract/README.md) | P02 | Demo040 | real | Validate a typed business draft and request missing information. |
| [Demo060 — Tools Catalog](../demos/Demo060_ToolsCatalog/README.md) | P03 | Demo050 | real | Resolve real catalog items through host-controlled tools. |
| [Demo070 — SQLite Backlog](../demos/Demo070_SqliteBacklog/README.md) | P04 | Demo060 | real | Inspect and persist a validated business item in SQLite. |
| [Demo080 — Approved Backlog](../demos/Demo080_ApprovedBacklog/README.md) | P04 | Demo070 | real | Compare explicit in-process approval and refusal before a SQLite write. |
| [Demo090 — Complete Pipeline](../demos/Demo090_CompletePipeline/README.md) | P04 | Demo080 | real | Connect intake, validation, tools and approved action. |
| [Demo100 — Controlled Action](../demos/Demo100_ControlledAction/README.md) | P04 | Demo050, Demo060 | real | Compare prompt-only instructions with enforced host validation. |
| [Demo110 — Tool Reliability](../demos/Demo110_ToolReliability/README.md) | P04 | Demo060 | real | Compare ambiguous and precise tool descriptions against observed invocation payloads. |
| [Demo120 — Tool Hallucination](../demos/Demo120_ToolHallucination/README.md) | P04 | Demo060 | real | Reject invented tool results and inspect actual business effects. |
| [Demo130 — Session Persistence](../demos/Demo130_SessionPersistence/README.md) | P05 | Demo040 | real | Persist business conversation state and reload it across launches. |
| [Demo140 — Native Session](../demos/Demo140_NativeSession/README.md) | P05 | Demo130 | real | Compare native serialized agent sessions with explicit conversation state. |
| [Demo150 — Shared Facts](../demos/Demo150_SharedFacts/README.md) | P05 | Demo130 | real | Share controlled facts with provenance and an explicit forgetting lifecycle. |
| [Demo160 — Context Memory](../demos/Demo160_ContextMemory/README.md) | P05 | Demo150 | real | Inject remembered facts through the native AI context provider. |
| [Demo170 — Memory Drift](../demos/Demo170_MemoryDrift/README.md) | P05 | Demo150 | real | Observe corrections and prevent stale facts from silently replacing current facts. |
| [Demo180 — Vector Ingestion](../demos/Demo180_VectorIngestion/README.md) | P06 | Demo050, Demo060 | real | Build dense embeddings and a versioned SQLite vector index. |
| [Demo190 — Retrieved Evidence](../demos/Demo190_RetrievedEvidence/README.md) | P06 | Demo180 | real | Retrieve eligible vector matches and validate cited evidence. |
| [Demo200 — Retrieval Provider](../demos/Demo200_RetrievalProvider/README.md) | P06 | Demo190 | real | Compare retrieval through a native context provider. |
| [Demo210 — Corpus Quality](../demos/Demo210_CorpusQuality/README.md) | P06 | Demo180 | real | Compare corpus preparation and actual dense-vector retrieval outcomes. |
| [Demo220 — Business Retrieval](../demos/Demo220_BusinessRetrieval/README.md) | P06 | Demo190 | real | Apply business eligibility before vector ranking and inspect refusal. |
| [Demo230 — Typed Workflow](../demos/Demo230_TypedWorkflow/README.md) | P07 | Demo050, Demo060 | deterministic | Route typed messages through a native deterministic MAF graph. |
| [Demo240 — Agent Workflow](../demos/Demo240_AgentWorkflow/README.md) | P07 | Demo230, Demo040 | real | Insert a native linguistic agent step into the typed graph. |
| [Demo250 — Workflow Events](../demos/Demo250_WorkflowEvents/README.md) | P07 | Demo230 | real | Observe native workflow events and distinguish them from business proof. |
| [Demo260 — Multi Agent Roles](../demos/Demo260_MultiAgentRoles/README.md) | P07 | Demo240 | real | Compare explicit role orchestration with graph execution. |
| [Demo270 — Planning](../demos/Demo270_Planning/README.md) | P07 | Demo260 | real | Treat a generated plan as data requiring host execution. |
| [Demo280 — Validated Planning](../demos/Demo280_ValidatedPlanning/README.md) | P07 | Demo270 | real | Reject invalid plans before executing host operations. |
| [Demo290 — Formatted Planning](../demos/Demo290_FormattedPlanning/README.md) | P07 | Demo280 | real | Add a structured format contract to validated planning. |
| [Demo300 — External Request](../demos/Demo300_ExternalRequest/README.md) | P08 | Demo230 | deterministic | Pause a graph for an explicit external response. |
| [Demo310 — Checkpoint Resume](../demos/Demo310_CheckpointResume/README.md) | P08 | Demo300 | deterministic | Reload a checkpoint in another process and resume the pending request. |
| [Demo320 — Durable Approval](../demos/Demo320_DurableApproval/README.md) | P08 | Demo310 | deterministic | Approve or refuse an immutable proposal and replay a unique SQLite effect. |
| [Demo330 — Bounded Harness](../demos/Demo330_BoundedHarness/README.md) | P09 | Demo060, Demo230 | deterministic | Enforce iteration, time and cancellation limits on an actual loop. |
| [Demo340 — Loop Control](../demos/Demo340_LoopControl/README.md) | P09 | Demo330 | deterministic | Detect repeated requests and terminate a non-progressing agent loop. |
| [Demo350 — Graceful Degradation](../demos/Demo350_GracefulDegradation/README.md) | P09 | Demo330 | deterministic | Return an explicit degraded outcome on timeout or dependency failure. |
| [Demo360 — Prompt Injection](../demos/Demo360_PromptInjection/README.md) | P09 | Demo060 | real | Inspect untrusted input and enforce the host trust boundary. |
| [Demo370 — Layered Protection](../demos/Demo370_LayeredProtection/README.md) | P09 | Demo360 | real | Compare layered prompt instructions with independent host controls. |
| [Demo380 — Role Based Tools](../demos/Demo380_RoleBasedTools/README.md) | P09 | Demo060 | deterministic | Filter exposed tools and enforce authorization at execution time. |
| [Demo390 — Prompt Profiles](../demos/Demo390_PromptProfiles/README.md) | P09 | Demo050 | real | Select explicit prompt profiles without bypassing business contracts. |
| [Demo400 — Local MCP](../demos/Demo400_LocalMcp/README.md) | P10 | Demo060 | deterministic | Discover and invoke the precompiled local stdio MCP server. |
| [Demo410 — Streaming Interfaces](../demos/Demo410_StreamingInterfaces/README.md) | P11 | Demo130, Demo400 | real | Use a shared streaming backend from console, Web and WPF clients with persistent sessions. |
| [Demo420 — Safe Telemetry](../demos/Demo420_SafeTelemetry/README.md) | P12 | Demo250, Demo330 | deterministic | Correlate safe native and HTTP spans and record nullable measured usage. |
| [Demo430 — Business Audit](../demos/Demo430_BusinessAudit/README.md) | P12 | Demo320, Demo420 | deterministic | Reopen the separate business journal and distinguish effects from traces. |
| [Demo440 — Context Budget](../demos/Demo440_ContextBudget/README.md) | P12 | Demo150, Demo420 | real | Measure extraction, summary and compaction overhead separately. |
| [Demo450 — Contract Regression](../demos/Demo450_ContractRegression/README.md) | P13 | Demo050 | real | Run deterministic business contract regressions before model evaluation. |
| [Demo460 — Candidate Evaluation](../demos/Demo460_CandidateEvaluation/README.md) | P13 | Demo190, Demo450 | fixture | Evaluate isolated candidate answers against versioned references and strict gates. |
| [Demo470 — Documented Approval](../demos/Demo470_DocumentedApproval/README.md) | P14 | Demo220, Demo320, Demo430 | fixture | Bind validated RAG evidence to a durable approved proposal and unique business effect. |
| [Demo480 — Model Routing](../demos/Demo480_ModelRouting/README.md) | P15 | Demo330, Demo460 | real | Compare routed requests including triage overhead against a direct baseline. |
| [Demo490 — Production Review](../demos/Demo490_ProductionReview/README.md) | P15 | Demo470 | deterministic | Review the remaining application-specific deployment decisions. |
