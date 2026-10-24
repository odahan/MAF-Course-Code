# Demo420 — Safe Telemetry

Correlate safe native and HTTP spans and record nullable measured usage.

Prerequisites: [Demo250](../../demos/Demo250_WorkflowEvents/README.md), [Demo330](../../demos/Demo330_BoundedHarness/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo420.App.csproj](Demo420.App.csproj) (App).

Execution mode: **deterministic**. No model for the documented default command. Optional live modes require the shared connection profile.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo420_SafeTelemetry/Demo420.App.csproj -c Release --no-build -- --deterministic
```

Observe: Correlate safe native and HTTP spans and record nullable measured usage. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo430 — Business Audit](../Demo430_BusinessAudit/README.md).

Read the [operational guide](../../docs/lot-013/Observability.md) for scenario arguments, additional modes and refusal cases.
