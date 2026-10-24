# Demo230 — Typed Workflow

Route typed messages through a native deterministic MAF graph.

Prerequisites: [Demo050](../../demos/Demo050_StructuredBusinessContract/README.md), [Demo060](../../demos/Demo060_ToolsCatalog/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo230.App.csproj](Demo230.App.csproj) (App).

Execution mode: **deterministic**. No model for the documented default command. Optional live modes require the shared connection profile.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo230_TypedWorkflow/Demo230.App.csproj -c Release --no-build
```

Observe: Route typed messages through a native deterministic MAF graph. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo240 — Agent Workflow](../Demo240_AgentWorkflow/README.md).

Read the [operational guide](../../docs/lot-008/Workflows.md) for scenario arguments, additional modes and refusal cases.
