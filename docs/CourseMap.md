# Technical course map

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Chapter numbers may change; Demo identifiers remain stable. Prerequisites are the explicit demo edges in the catalogue. This map specifies no days, duration or editorial calendar. The [global guide](lot-015/Guide.md) supplies configuration and acceptance limits.

Before P00–P15, run [00-ConfigurateurDemo](../demos/00-ConfigurateurDemo/README.md) to select the chat and embedding profiles, inspect effective settings and optionally test connections. This setup utility is separate from the 49 stable learning demo identifiers. Saved central settings are read by newly started programs without recompiling them.

| Section | Marker | Main demonstrations and deepening | Exercises and prerequisites |
|---|---|---|---|
| 1. Code Or Agent | P00 | [Demo010 — Code Or Agent](../demos/Demo010_CodeOrAgent/README.md) | Follow catalogue prerequisites; inspect the observable transition before continuing. |
| 2. Connection Profiles | P01 | [Demo020 — Connection Profiles](../demos/Demo020_ConnectionProfiles/README.md); [Demo030 — Agent Capabilities](../demos/Demo030_AgentCapabilities/README.md); [Demo040 — First Agent](../demos/Demo040_FirstAgent/README.md) | Follow catalogue prerequisites; inspect the observable transition before continuing. |
| 3. Structured Business Contract | P02 | [Demo050 — Structured Business Contract](../demos/Demo050_StructuredBusinessContract/README.md) | [Foundations](../exercises/Foundations/README.md); Actionability Start/End after Demo050 and Demo080 |
| 4. Tools Catalog | P03 | [Demo060 — Tools Catalog](../demos/Demo060_ToolsCatalog/README.md) | [Foundations](../exercises/Foundations/README.md); Actionability Start/End after Demo050 and Demo080 |
| 5. Sqlite Backlog | P04 | [Demo070 — Sqlite Backlog](../demos/Demo070_SqliteBacklog/README.md); [Demo080 — Approved Backlog](../demos/Demo080_ApprovedBacklog/README.md); [Demo090 — Complete Pipeline](../demos/Demo090_CompletePipeline/README.md); [Demo100 — Controlled Action](../demos/Demo100_ControlledAction/README.md); [Demo110 — Tool Reliability](../demos/Demo110_ToolReliability/README.md); [Demo120 — Tool Hallucination](../demos/Demo120_ToolHallucination/README.md) | [Foundations](../exercises/Foundations/README.md); Actionability Start/End after Demo050 and Demo080 |
| 6. Session Persistence | P05 | [Demo130 — Session Persistence](../demos/Demo130_SessionPersistence/README.md); [Demo140 — Native Session](../demos/Demo140_NativeSession/README.md); [Demo150 — Shared Facts](../demos/Demo150_SharedFacts/README.md); [Demo160 — Context Memory](../demos/Demo160_ContextMemory/README.md); [Demo170 — Memory Drift](../demos/Demo170_MemoryDrift/README.md) | [Advanced guided manipulations](../exercises/Advanced/README.md), after the corresponding main demo |
| 7. Vector Ingestion | P06 | [Demo180 — Vector Ingestion](../demos/Demo180_VectorIngestion/README.md); [Demo190 — Retrieved Evidence](../demos/Demo190_RetrievedEvidence/README.md); [Demo200 — Retrieval Provider](../demos/Demo200_RetrievalProvider/README.md); [Demo210 — Corpus Quality](../demos/Demo210_CorpusQuality/README.md); [Demo220 — Business Retrieval](../demos/Demo220_BusinessRetrieval/README.md) | [Advanced guided manipulations](../exercises/Advanced/README.md), after the corresponding main demo |
| 8. Typed Workflow | P07 | [Demo230 — Typed Workflow](../demos/Demo230_TypedWorkflow/README.md); [Demo240 — Agent Workflow](../demos/Demo240_AgentWorkflow/README.md); [Demo250 — Workflow Events](../demos/Demo250_WorkflowEvents/README.md); [Demo260 — Multi Agent Roles](../demos/Demo260_MultiAgentRoles/README.md); [Demo270 — Planning](../demos/Demo270_Planning/README.md); [Demo280 — Validated Planning](../demos/Demo280_ValidatedPlanning/README.md); [Demo290 — Formatted Planning](../demos/Demo290_FormattedPlanning/README.md) | Follow catalogue prerequisites; inspect the observable transition before continuing. |
| 9. External Request | P08 | [Demo300 — External Request](../demos/Demo300_ExternalRequest/README.md); [Demo310 — Checkpoint Resume](../demos/Demo310_CheckpointResume/README.md); [Demo320 — Durable Approval](../demos/Demo320_DurableApproval/README.md) | Follow catalogue prerequisites; inspect the observable transition before continuing. |
| 10. Bounded Harness | P09 | [Demo330 — Bounded Harness](../demos/Demo330_BoundedHarness/README.md); [Demo340 — Loop Control](../demos/Demo340_LoopControl/README.md); [Demo350 — Graceful Degradation](../demos/Demo350_GracefulDegradation/README.md); [Demo360 — Prompt Injection](../demos/Demo360_PromptInjection/README.md); [Demo370 — Layered Protection](../demos/Demo370_LayeredProtection/README.md); [Demo380 — Role Based Tools](../demos/Demo380_RoleBasedTools/README.md); [Demo390 — Prompt Profiles](../demos/Demo390_PromptProfiles/README.md) | Follow catalogue prerequisites; inspect the observable transition before continuing. |
| 11. Local Mcp | P10 | [Demo400 — Local Mcp](../demos/Demo400_LocalMcp/README.md) | Follow catalogue prerequisites; inspect the observable transition before continuing. |
| 12. Streaming Interfaces | P11 | [Demo410 — Streaming Interfaces](../demos/Demo410_StreamingInterfaces/README.md) | Follow catalogue prerequisites; inspect the observable transition before continuing. |
| 13. Safe Telemetry | P12 | [Demo420 — Safe Telemetry](../demos/Demo420_SafeTelemetry/README.md); [Demo430 — Business Audit](../demos/Demo430_BusinessAudit/README.md); [Demo440 — Context Budget](../demos/Demo440_ContextBudget/README.md) | [Advanced guided manipulations](../exercises/Advanced/README.md), after the corresponding main demo |
| 14. Contract Regression | P13 | [Demo450 — Contract Regression](../demos/Demo450_ContractRegression/README.md); [Demo460 — Candidate Evaluation](../demos/Demo460_CandidateEvaluation/README.md) | Follow catalogue prerequisites; inspect the observable transition before continuing. |
| 15. Documented Approval | P14 | [Demo470 — Documented Approval](../demos/Demo470_DocumentedApproval/README.md) | Follow catalogue prerequisites; inspect the observable transition before continuing. |
| 16. Model Routing | P15 | [Demo480 — Model Routing](../demos/Demo480_ModelRouting/README.md); [Demo490 — Production Review](../demos/Demo490_ProductionReview/README.md) | Follow catalogue prerequisites; inspect the observable transition before continuing. |

The [exercise registry](lot-015/Exercises.md) distinguishes runnable Start/End from guided manipulations of complete programs. Demo470 is the integrated endpoint, with Demo480–Demo490 optional extensions. The default granite4:3b candidate/judge profile remains unqualified.

## Main path and deepening

The main reference path is Demo010 → Demo020–Demo040 → Demo050–Demo060 → Demo080–Demo090 → Demo130 and Demo150 → Demo180–Demo190 → Demo230–Demo240 → Demo310–Demo320 → Demo330 → Demo400 → Demo410 → Demo420–Demo430 → Demo460 → Demo470. This is a reading order; the catalogue prerequisites specify the actual dependencies. Other demos in each section are independent comparisons or deepening. Demo480 and Demo490 are optional extensions. A completed variant keeps its own identifier even when a course omits it.

## Exercise correspondence

| Exercise / manipulation | Demo prerequisites | Current procedure and observable result |
|---|---|---|
| Actionability Start / End | Demo050, Demo080 | [Start/End](../exercises/Actionability/README.md): analysis accepts the fixture, action requires clarification; Start exits 2, End exits 0. |
| Fact correction and forgetting | Demo130, Demo150; compare Demo160 | [Memory procedure](lot-006/SessionsAndMemory.md): correction replaces the canonical fact; forgetting remains effective after restart. |
| Citation validation | Demo180, Demo190; compare Demo200 and Demo220 | [RAG procedure](lot-007/CorpusAndRetrieval.md): rebuild real dense embeddings, inspect eligible cosine matches and reject fabricated quotation evidence. |
| Typed branch and plan validation | Demo230, Demo240; compare Demo270–Demo290 | [Workflow procedure](lot-008/Workflows.md): incomplete data follows clarification; a plan does not execute itself. |
| Durable rejection, restart and replay | Demo310, Demo320 | [Durable procedure](lot-009/DurableApproval.md): approval produces one stored effect; refusal produces none; replay preserves its identifier. |
| Bounded execution and cancellation | Demo330; compare Demo340–Demo350 | [Control procedure](lot-010/ControlsAndHarness.md): repeat, permission, budget and deadline outcomes are explicit. |
| MCP rediscovery | Demo060, Demo400 | [MCP procedure](lot-011/LocalMcp.md): restart the rebuilt server and inspect the actual discovered schema and returned payload. |
| Stream fidelity, session reuse and cancellation | Demo130, Demo400, Demo410 | [Streaming procedure](lot-012/Streaming.md): exact whitespace and terminal frames; incomplete streams never count as completed turns. |
| Nullable usage and business journal | Demo320, Demo330, Demo420, Demo430 | [Observability procedure](lot-013/Observability.md): separate model usage, estimates, traces and reopened business effects. |
| Candidate isolation and calibration | Demo190, Demo450, Demo460 | [Evaluation exercises](lot-014/Exercises.md): candidate inputs contain no expected answer; calibration failure prevents qualification. |
| Integrated documented action | Demo220, Demo320, Demo430, then Demo470 | [Scenario](lot-015/Scenario.md): evidence is bound to the exact immutable draft before external approval. |

These are guided manipulations of complete programs except for the explicit Actionability pair. Historical exercise filenames and solutions remain traceable through the registry and provenance map.
