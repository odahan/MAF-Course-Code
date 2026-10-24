# Demo270 — Planning

Treat a generated plan as data requiring host execution.

Prerequisites: [Demo260](../../demos/Demo260_MultiAgentRoles/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo270.App.csproj](Demo270.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo270_Planning/Demo270.App.csproj -c Release --no-build
```

Observe: Treat a generated plan as data requiring host execution. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo280 — Validated Planning](../Demo280_ValidatedPlanning/README.md).

Read the [operational guide](../../docs/lot-008/Workflows.md) for scenario arguments, additional modes and refusal cases.
