# Demo320 — Durable Approval

Approve or refuse an immutable proposal and replay a unique SQLite effect.

Prerequisites: [Demo310](../../demos/Demo310_CheckpointResume/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo320.App.csproj](Demo320.App.csproj) (App).

Execution mode: **deterministic**. No model for the documented default command. Optional live modes require the shared connection profile.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
$state = Join-Path $env:TEMP ('maf-demo320-' + [guid]::NewGuid().ToString('N'))
dotnet run --project demos/Demo320_DurableApproval/Demo320.App.csproj -c Release --no-build -- start $state
dotnet run --project demos/Demo320_DurableApproval/Demo320.App.csproj -c Release --no-build -- resume $state approve
dotnet run --project demos/Demo320_DurableApproval/Demo320.App.csproj -c Release --no-build -- status $state
```

Observe: Approve or refuse an immutable proposal and replay a unique SQLite effect. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo330 — Bounded Harness](../Demo330_BoundedHarness/README.md).

Read the [operational guide](../../docs/lot-009/DurableApproval.md) for scenario arguments, additional modes and refusal cases.
