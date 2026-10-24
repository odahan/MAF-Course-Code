# Demo480 — Model Routing

Compare routed requests including triage overhead against a direct baseline.

Prerequisites: [Demo330](../../demos/Demo330_BoundedHarness/README.md), [Demo460](../../demos/Demo460_CandidateEvaluation/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo480.App.csproj](Demo480.App.csproj) (App).

Execution mode: **real**. Ollama only. MAF_ROUTE_TRIAGE, MAF_ROUTE_EASY and MAF_ROUTE_HARD optionally select already installed model names. Identical defaults demonstrate mechanics only; --sensitive escalates without a model call.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo480_ModelRouting/Demo480.App.csproj -c Release --no-build
```

Observe: Compare routed requests including triage overhead against a direct baseline. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo490 — Production Review](../Demo490_ProductionReview/README.md).

Read the [operational guide](../../docs/lot-013/Observability.md) for scenario arguments, additional modes and refusal cases.
