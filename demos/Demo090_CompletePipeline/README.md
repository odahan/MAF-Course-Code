# Demo090 — Complete Pipeline

Connect intake, validation, tools and approved action.

Prerequisites: [Demo080](../../demos/Demo080_ApprovedBacklog/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo090.App.csproj](Demo090.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo090_CompletePipeline/Demo090.App.csproj -c Release --no-build
```

Observe: Connect intake, validation, tools and approved action. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: [build instructions](../../README.md); the acceptance record (unpublished local archive) distinguishes deterministic checks from dated live campaigns. Shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo100 — Controlled Action](../Demo100_ControlledAction/README.md).

## Scenario procedure


Renovated in Lot-005. This Markdown README supersedes historical filming notes and README text files.

Explicit host orchestration: trusted catalogue -> analyst without sensitive tools -> validated fixed draft -> native MAF approval -> real SQLite write. This is a C# pipeline, not a MAF workflow graph.

From `BaseTechnique`:

```powershell
dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore
dotnet run --project demos/Demo090_CompletePipeline/Demo090.App.csproj --configuration Release --no-build
```

The distributed `ai-settings.json` comes from the shared connection profile. Ollama `granite4:3b` is the default; environment `AI__...` and explicit `--AI:...` arguments override configuration. No automatic cloud fallback exists. For the optional EP002 flag, use `-- --agent` after `dotnet run` options.

Verification: All Maf.Backlog.Tests plus real Ollama accept/refuse logs. The stored fields match the reviewed draft. Model creation claims alone are not evidence. Logs are under `docs/lot-005`. Run the shared behavior suite with:

```powershell
dotnet test tests/Maf.Backlog.Tests/Maf.Backlog.Tests.csproj -m:1 --configuration Release
```

SQLite writer examples use a real local demo database. `MAF_DEMO_DB_PATH` selects an isolated file; `MAF_DEMO_READ_ONLY=1` denies writes on the host. This environment switch is a demo policy, not production authentication. Type `list` to inspect stored rows. A new proposal or restart is not durably deduplicated; durable resumption and idempotence belong to Lot-009.

See the [complete contract, approval API, exercise and limitations](../../docs/lot-005/ContractsAndTools.md). Original source material remains in the Lot-001 baseline and original source locations.
