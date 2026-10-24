# Demo450 — Contract Regression

Run deterministic business contract regressions before model evaluation.

Prerequisites: [Demo050](../../demos/Demo050_StructuredBusinessContract/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo450.App.csproj](Demo450.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo450_ContractRegression/Demo450.App.csproj -c Release --no-build
```

Observe: Run deterministic business contract regressions before model evaluation. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo460 — Candidate Evaluation](../Demo460_CandidateEvaluation/README.md).

## Scenario procedure


Renovated in Lot-005. This Markdown README supersedes historical filming notes and README text files.

Enter a support request. The caller owns the shared connection. The host validates the typed decision and routes risk scores >=70 to human_review. Classification never creates a ticket.

From `BaseTechnique`:

```powershell
dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore
dotnet run --project demos/Demo450_ContractRegression/Demo450.App.csproj --configuration Release --no-build
```

The distributed `ai-settings.json` comes from the shared connection profile. Ollama `granite4:3b` is the default; environment `AI__...` and explicit `--AI:...` arguments override configuration. No automatic cloud fallback exists. For the optional EP002 flag, use `-- --agent` after `dotnet run` options.

Verification: Five existing deterministic tests retained in tests/AgentCiCd.Tests; smoke-ep004.log. Contract identifiers and prompts are now English. Logs are under `docs/lot-005`. Run the shared behavior suite with:

```powershell
dotnet test tests/Maf.Backlog.Tests/Maf.Backlog.Tests.csproj -m:1 --configuration Release
```

SQLite writer examples use a real local demo database. `MAF_DEMO_DB_PATH` selects an isolated file; `MAF_DEMO_READ_ONLY=1` denies writes on the host. This environment switch is a demo policy, not production authentication. Type `list` to inspect stored rows. A new proposal or restart is not durably deduplicated; durable resumption and idempotence belong to Lot-009.

See the [complete contract, approval API, exercise and limitations](../../docs/lot-005/ContractsAndTools.md). Original source material remains in the Lot-001 baseline and original source locations.

For the console mode, enter a support request when prompted; the result is a classification and creates no ticket. Deterministic regression checks use `dotnet test tests/AgentCiCd.Tests/AgentCiCd.Tests.csproj -m:1 -c Release --no-build --no-restore`.
