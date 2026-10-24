# Demo310 — Checkpoint Resume

Reload a checkpoint in another process and resume the pending request.

Prerequisites: [Demo300](../../demos/Demo300_ExternalRequest/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo310.App.csproj](Demo310.App.csproj) (App).

Execution mode: **deterministic**. No model for the documented default command. Optional live modes require the shared connection profile.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo310_CheckpointResume/Demo310.App.csproj -c Release --no-build
```

Observe: Reload a checkpoint in another process and resume the pending request. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo320 — Durable Approval](../Demo320_DurableApproval/README.md).

Read the [operational guide](../../docs/lot-009/DurableApproval.md) for scenario arguments, additional modes and refusal cases.
