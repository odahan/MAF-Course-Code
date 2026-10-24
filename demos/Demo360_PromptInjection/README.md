# Demo360 — Prompt Injection

Inspect untrusted input and enforce the host trust boundary.

Prerequisites: [Demo060](../../demos/Demo060_ToolsCatalog/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo360.App.csproj](Demo360.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo360_PromptInjection/Demo360.App.csproj -c Release --no-build
```

Observe: Inspect untrusted input and enforce the host trust boundary. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: [build instructions](../../README.md); the acceptance record (unpublished local archive) distinguishes deterministic checks from dated live campaigns. Shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo370 — Layered Protection](../Demo370_LayeredProtection/README.md).

## Scenario procedure


Renovated in Lot-005. This Markdown README supersedes historical filming notes and README text files.

Decode and summarize an untrusted Base64 document with a tool-free agent. Encoding does not enforce authorization or guarantee model accuracy. Continue with the two-layer example.

From `BaseTechnique`:

```powershell
dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore
dotnet run --project demos/Demo360_PromptInjection/Demo360.App.csproj --configuration Release --no-build
```

The distributed `ai-settings.json` comes from the shared connection profile. Ollama `granite4:3b` is the default; environment `AI__...` and explicit `--AI:...` arguments override configuration. No automatic cloud fallback exists. For the optional EP002 flag, use `-- --agent` after `dotnet run` options.

Verification: smoke-ep003-spotlight.log; no sensitive tool exists. The model can misread the document; this is not an accuracy benchmark. Logs are under `docs/lot-005`. Run the shared behavior suite with:

```powershell
dotnet test tests/Maf.Backlog.Tests/Maf.Backlog.Tests.csproj -m:1 --configuration Release
```

SQLite writer examples use a real local demo database. `MAF_DEMO_DB_PATH` selects an isolated file; `MAF_DEMO_READ_ONLY=1` denies writes on the host. This environment switch is a demo policy, not production authentication. Type `list` to inspect stored rows. A new proposal or restart is not durably deduplicated; durable resumption and idempotence belong to Lot-009.

See the [complete contract, approval API, exercise and limitations](../../docs/lot-005/ContractsAndTools.md). Original source material remains in the Lot-001 baseline and original source locations.
