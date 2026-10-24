# Demo220 — Business Retrieval

Apply business eligibility before vector ranking and inspect refusal.

Prerequisites: [Demo190](../../demos/Demo190_RetrievedEvidence/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo220.App.csproj](Demo220.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo220_BusinessRetrieval/Demo220.App.csproj -c Release --no-build
```

Observe: Apply business eligibility before vector ranking and inspect refusal. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo230 — Typed Workflow](../Demo230_TypedWorkflow/README.md).

Read the [operational guide](../../docs/lot-007/CorpusAndRetrieval.md) for scenario arguments, additional modes and refusal cases.

In the console, run `rebuild` before querying a new index, then `audit` to inspect it. Use an isolated absolute `MAF_STATE_ROOT` for exercises. An embedding-model change requires rebuilding; missing or incompatible vectors fail explicitly.
