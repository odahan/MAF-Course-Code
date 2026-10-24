# Demo280 — Validated Planning

Reject invalid plans before executing host operations.

Prerequisites: [Demo270](../../demos/Demo270_Planning/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo280.App.csproj](Demo280.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo280_ValidatedPlanning/Demo280.App.csproj -c Release --no-build
```

Observe: Reject invalid plans before executing host operations. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo290 — Formatted Planning](../Demo290_FormattedPlanning/README.md).

Read the [operational guide](../../docs/lot-008/Workflows.md) for scenario arguments, additional modes and refusal cases.
