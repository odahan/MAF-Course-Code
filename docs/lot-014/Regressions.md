# Consolidated regression coverage

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

The canonical solution and Build.ps1 keep the existing tests active; the lot does not replace application guarantees with model opinion. All tests listed here execute real host code with controlled model outputs. Local MCP and streaming tests also run real local transports.

| Guarantee | Executable evidence |
|---|---|
| Typed/business contracts, forbidden actions and missing fields | AgentCiCd.Tests; Maf.Backlog.Tests.DeserializableInvalidBusinessContractCannotWrite; Evaluation.Tests contract data rows |
| Approval/refusal, permission and proposal binding | Maf.Backlog.Tests.NativeMafApprovalControlsActualSqliteWrite; HostPermissionAndExactProposalRemainRequired |
| Missing tool data and no invented success | Maf.Backlog.Tests.MissingRefusedAndErroredLookupNeverInventAValue; GeneratedCreationClaimWithoutToolIsNotEvidence |
| Persistent memory, reset, correction, forgetting and stale updates | Maf.Memory.Tests.RestartPreservesSqliteContextAndTransactionalResetWorksWithPooling; ForgottenOrOutOfOrderUpdatesCannotResurrectFacts |
| Real dense vectors and evidence governance in SQLite | Maf.Rag.Tests.ReopeningSqlitePreservesActualDenseVectors; CosineIsRealAndRejectsIncompatibleVectors; FabricatedCitationsAndAlteredQuotesAreRejected; MissingEvidenceRefusesBeforeCreatingAnyModelClient |
| Native graph branches and postvalidation | Maf.Workflows.Tests.NativeAgentRunsOnceWithSchemaAndHostPostValidation; NativeAgentOutputIsNeverTrustedWithoutValidation |
| Durable resume and no duplicate business effect | Maf.Durable.Tests.FileCheckpointReemitsTheSameRequestAndReplaysWithoutDuplicate; ConcurrentDuplicateEffectsStillCreateOneRow |
| Permissions, budgets, native Harness and cancellation | Maf.Control.Tests.DirectInvocationCannotBypassPermission; RealHarnessLoopHasExplicitTerminalState; CallerCancellationThroughNativeHarnessPropagates |
| Streaming fidelity, cancellation and no partial commit | Streaming.Tests.ExactWhitespaceAndUnicodeSurviveHttpAndSessionRestart; ClientCancellationReachesModelAndDoesNotSave; MidstreamErrorIsVisibleAndDoesNotCommitPartialTurn |
| Local MCP protocol, error handling and restart | LocalMcp.Tests (22 cases) |
| Correlated traces, committed journal evidence and nullable usage | Maf.Observability.Tests.NativeAgentToolLoopHasSafeCorrelatedSpans; IntentAndFailureDoNotProveAction; MissingUsageIsNotZero |
| Candidate isolation, strict schemas, failed calibration semantics and critical gates | Episode17Demo.Tests.EvaluationTests and ModelRunnerTests |

TRX files in test-results and the test logs record actual outcomes. Tests from the unchanged prior lots need not be rerun for every documentation edit. The final evaluation tests are rerun after fixes to the evaluator. Live model behavior is reported separately in runs; no fixture or test double is called a measured model result.

