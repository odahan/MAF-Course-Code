# Demo190 — Retrieved Evidence

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Retrieve eligible vector matches and validate cited evidence.

Prerequisites: [Demo180](../../demos/Demo180_VectorIngestion/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo190.App.csproj](Demo190.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo190_RetrievedEvidence/Demo190.App.csproj -c Release --no-build
```

Observe: Retrieve eligible vector matches and validate cited evidence. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo200 — Retrieval Provider](../Demo200_RetrievalProvider/README.md).

## Scenario procedure


Renovated in Lot-007 for .NET 10, the common AI connection and Microsoft.Data.Sqlite. No external vector service is required.

Run the canonical dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore, then tools/Verify-Lot007.ps1. The interactive commands are rebuild, audit, compare, probe <question>, and quit. EOF exits.

See [the current guide](../../docs/lot-007/CorpusAndRetrieval.md) for commands, calibration, governance, source validation and acceptance evidence.


In the console, run `rebuild` before querying a new index, then `audit` to inspect it. Use an isolated absolute `MAF_STATE_ROOT` for exercises. An embedding-model change requires rebuilding; missing or incompatible vectors fail explicitly.
