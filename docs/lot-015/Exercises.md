# Current exercises and historical disposition

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

The distribution has one selected code-completion pair: [Actionability](../../exercises/Actionability/README.md). Both projects build independently and belong to the canonical solution. Start prints an explicit incomplete outcome and exits 2. End compares analysis validation with action validation, then validates a newly clarified draft; it exits 0 for the expected observations. Neither project writes data or removes an approval guard. Implement the comparison in a working copy of Start; compare its output with End.

```powershell
dotnet run --project exercises/Actionability/Start -c Release --no-build
dotnet run --project exercises/Actionability/End -c Release --no-build
```

Retained guided manipulations use complete working examples and executable tests as references. They are not advertised as unfinished Start projects. The following decisions replace the historical Lot-001 exercise backlog for distribution:

| Historical exercise | Current decision and verified reference |
|---|---|
| J1 ClarifyingQuestions | Replaced by the Actionability Start/End pair. The learner compares a valid analysis with a draft that cannot authorize action while a question remains. No automatic clarification dialogue is promised. |
| J1 AddComponent | Retired as a file-edit exercise: `components.json` is historical; host `Catalog.Names` controls the allowlist. Inspect the catalogue and change host policy plus tests only in a working copy. [Contract guide](../lot-005/ContractsAndTools.md). |
| J1 StatusTool | Retired: no current status tool is distributed. The old Task<bool> solution is not a current solution. Study typed Found/NotFound/Refused/Error results and actual writes instead. |
| J1 ResetAndList | Replaced by guided approval/refusal/readback. No reset-at-start is introduced. [Contract guide](../lot-005/ContractsAndTools.md). |
| J2 Rag_SourcesFormat | Replaced by actual citation gates and the fabricated-citation rejection in [Scenario.md](Scenario.md). Inspect `Maf.Rag.Tests` and use the complete RAG programs; they are not Start code. |
| J2 SharedMemory_Facts | Retained guided declaration/correction/bounds manipulation; [memory guide](../lot-006/SessionsAndMemory.md) and `Maf.Memory.Tests`. No separate Start/End pair is promised. |
| J2 SharedMemory_Reuse | Retained guided session isolation and coordinated forget/restart; same guide and actual SQLite tests. |
| J2 Otel_Tokens | Retained guided nullable/partial usage comparison; [observability guide](../lot-013/Observability.md) and `Maf.Observability.Tests`. |
| J2 Otel_ToolAndHttp | Retained guided correlated native spans and separately reopened business journal; same guide and Lot-013 receipts. |

Further guided exercises: [native branches](../lot-008/Workflows.md), [durable reject/replay](../lot-009/DurableApproval.md), [budget/cancellation](../lot-010/ControlsAndHarness.md), [MCP rediscovery](../lot-011/LocalMcp.md), [stream fidelity/session restart](../lot-012/Streaming.md), [candidate isolation](../lot-014/Exercises.md). Each guide identifies its prerequisites and observable result. Historical individual exercise files and old solution directories remain provenance. Use their current index to reach this registry; do not distribute historical ZIPs as the renovated exercises.
