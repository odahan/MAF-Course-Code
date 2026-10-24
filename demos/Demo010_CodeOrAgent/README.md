# Demo010 — Code Or Agent

Compare deterministic routing, C# orchestration and optional open-ended reasoning.

Prerequisites: C# basics, async/await and configuration; no model needed for the default mode.. See the [course map](../../docs/CourseMap.md).

Projects: [Demo010.App.csproj](Demo010.App.csproj) (App).

Execution mode: **deterministic**. No model for the documented default command. Optional live modes require the shared connection profile.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo010_CodeOrAgent/Demo010.App.csproj -c Release --no-build
```

Observe: Compare deterministic routing, C# orchestration and optional open-ended reasoning. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: [build instructions](../../README.md); the acceptance record (unpublished local archive) distinguishes deterministic checks from dated live campaigns. Shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo020 — Connection Profiles](../Demo020_ConnectionProfiles/README.md).

## Scenario procedure


For the native MAF graph comparison, continue with [Demo230 — Typed Workflow](../Demo230_TypedWorkflow/README.md) and the [Lot-008 guide](../../docs/lot-008/Workflows.md). EP002 illustrates the architecture choice with ordinary C#; Video2 adds typed graph routing, a native agent host and workflow events.

Renovated in Lot-005. This Markdown README supersedes historical filming notes and README text files.

By default run deterministic C# only. Add --agent to run the optional open-ended agent branch. The explicit orchestration is ordinary C#, not a native MAF graph.

From `BaseTechnique`:

```powershell
dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore
dotnet run --project demos/Demo010_CodeOrAgent/Demo010.App.csproj --configuration Release --no-build
```

The distributed `ai-settings.json` comes from the shared connection profile. Ollama `granite4:3b` is the default; environment `AI__...` and explicit `--AI:...` arguments override configuration. No automatic cloud fallback exists. For the optional EP002 flag, use `-- --agent` after `dotnet run` options.

Verification: smoke-ep002.log verifies the deterministic branch. The optional agent branch uses the shared connection. Logs are under `docs/lot-005`. Run the shared behavior suite with:

```powershell
dotnet test tests/Maf.Backlog.Tests/Maf.Backlog.Tests.csproj -m:1 --configuration Release
```

SQLite writer examples use a real local demo database. `MAF_DEMO_DB_PATH` selects an isolated file; `MAF_DEMO_READ_ONLY=1` denies writes on the host. This environment switch is a demo policy, not production authentication. Type `list` to inspect stored rows. A new proposal or restart is not durably deduplicated; durable resumption and idempotence belong to Lot-009.

See the [complete contract, approval API, exercise and limitations](../../docs/lot-005/ContractsAndTools.md). Original source material remains in the Lot-001 baseline and original source locations.
