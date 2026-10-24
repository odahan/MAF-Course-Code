# Demo050 — Structured Business Contract

Validate a typed business draft and request missing information.

Prerequisites: [Demo040](../../demos/Demo040_FirstAgent/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo050.App.csproj](Demo050.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo050_StructuredBusinessContract/Demo050.App.csproj -c Release --no-build
```

Observe: Validate a typed business draft and request missing information. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: [build instructions](../../README.md); the acceptance record (unpublished local archive) distinguishes deterministic checks from dated live campaigns. Shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo060 — Tools Catalog](../Demo060_ToolsCatalog/README.md).

## Scenario procedure


Renovated in Lot-005. This Markdown README supersedes historical filming notes and README text files.

Native JSON schema followed by host business validation. Enter a clear bug, then try an ambiguous request. No write tool is exposed.

From `BaseTechnique`:

```powershell
dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore
dotnet run --project demos/Demo050_StructuredBusinessContract/Demo050.App.csproj --configuration Release --no-build
```

The distributed `ai-settings.json` comes from the shared connection profile. Ollama `granite4:3b` is the default; environment `AI__...` and explicit `--AI:...` arguments override configuration. No automatic cloud fallback exists. For the optional EP002 flag, use `-- --agent` after `dotnet run` options.

Verification: Eleven deserializable invalid business cases in Maf.Backlog.Tests; smoke-draft.log. Logs are under `docs/lot-005`. Run the shared behavior suite with:

```powershell
dotnet test tests/Maf.Backlog.Tests/Maf.Backlog.Tests.csproj -m:1 --configuration Release
```

SQLite writer examples use a real local demo database. `MAF_DEMO_DB_PATH` selects an isolated file; `MAF_DEMO_READ_ONLY=1` denies writes on the host. This environment switch is a demo policy, not production authentication. Type `list` to inspect stored rows. A new proposal or restart is not durably deduplicated; durable resumption and idempotence belong to Lot-009.

See the [complete contract, approval API, exercise and limitations](../../docs/lot-005/ContractsAndTools.md). Original source material remains in the Lot-001 baseline and original source locations.
