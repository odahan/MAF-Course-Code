# Documented request to durable decision

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

The host connects the existing RAG citation gate and the native MAF approval graph without granting authority to retrieved text. Build from the root as described in [Guide.md](Guide.md).

```powershell
dotnet run --project demos/Demo470_DocumentedApproval -c Release --no-build -- start ./state/proposal-01
dotnet run --project demos/Demo470_DocumentedApproval -c Release --no-build -- status ./state/proposal-01
dotnet run --project demos/Demo470_DocumentedApproval -c Release --no-build -- resume ./state/proposal-01 approve
dotnet run --project demos/Demo470_DocumentedApproval -c Release --no-build -- resume ./state/proposal-01 approve
```

The default fixture stores explicit two-dimensional dense vectors in SQLite and executes real cosine search, citation validation, MAF checkpointing and business transactions. Only embeddings and the answer proposal are controlled fixture inputs; they are not measured model outputs or lexical substitution. No model is called. For real embeddings and a native MAF answer use `start ./state/live-01 --live`, or run `./tools/Verify-Lot015.ps1 -Live`. The application uses the common environment connection overrides. Live refusal fails acceptance visibly; there is no fixture fallback.

The question and reference date are fixed for this teaching case: Reporting CSV export, 23 October 2026. The approved draft includes the exact host-validated quotation and chunk ID. A fresh directory is required. `vectors.db` stores the embedding index, `business.db` stores the immutable proposal and unique business row, `checkpoints` holds the native graph checkpoints, `waiting.json` identifies the suspended request, and `evidence.json` is a diagnostic answer copy. The durable proposal, rather than the editable diagnostic file, binds the decision to the evidence. Local state files are trusted host storage, not a tamper-resistant archive.

Stop after `start`: the business state must be Waiting with a null ItemId. Resume in a new process. `reject` must leave ItemId null; `approve` must create one row. An identical replay returns that ID; a conflicting decision fails. `maybe` fails before response. Start with `--invalid-evidence` in another directory to observe a fabricated citation rejected before approval storage is created. Run `Verify-Lot015.ps1` for these assertions and exercise checks from an unrelated working directory.

Each phase emits restricted traces through the shared exporter. The persistent OperationId links phase receipts and business reads. This is not the EP008 transactional review journal; the durable business database is the effect evidence. UI/MCP, forgetting, unavailable-provider handling, budgets and after-commit fault injection retain their separate established suites and receipts. See [AuditClosure.md](AuditClosure.md).
