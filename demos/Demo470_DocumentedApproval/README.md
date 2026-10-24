# Demo470 — Documented Approval

Bind validated RAG evidence to a durable approved proposal and unique business effect.

Prerequisites: [Demo220](../../demos/Demo220_BusinessRetrieval/README.md), [Demo320](../../demos/Demo320_DurableApproval/README.md), [Demo430](../../demos/Demo430_BusinessAudit/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo470.App.csproj](Demo470.App.csproj) (App).

Execution mode: **fixture**. No model for the documented default command. Optional live modes require the shared connection profile.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
$state = Join-Path $env:TEMP ('maf-demo470-' + [guid]::NewGuid().ToString('N'))
dotnet run --project demos/Demo470_DocumentedApproval/Demo470.App.csproj -c Release --no-build -- start $state
dotnet run --project demos/Demo470_DocumentedApproval/Demo470.App.csproj -c Release --no-build -- status $state
dotnet run --project demos/Demo470_DocumentedApproval/Demo470.App.csproj -c Release --no-build -- resume $state approve
dotnet run --project demos/Demo470_DocumentedApproval/Demo470.App.csproj -c Release --no-build -- resume $state approve
```

Observe: Bind validated RAG evidence to a durable approved proposal and unique business effect. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo480 — Model Routing](../Demo480_ModelRouting/README.md).

Read the [operational guide](../../docs/lot-015/Scenario.md) for scenario arguments, additional modes and refusal cases.
