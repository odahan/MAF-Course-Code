# Demo240 — Agent Workflow

Insert a native linguistic agent step into the typed graph.

Prerequisites: [Demo230](../../demos/Demo230_TypedWorkflow/README.md), [Demo040](../../demos/Demo040_FirstAgent/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo240.App.csproj](Demo240.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo240_AgentWorkflow/Demo240.App.csproj -c Release --no-build
```

Observe: Insert a native linguistic agent step into the typed graph. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo250 — Workflow Events](../Demo250_WorkflowEvents/README.md).

Read the [operational guide](../../docs/lot-008/Workflows.md) for scenario arguments, additional modes and refusal cases.
