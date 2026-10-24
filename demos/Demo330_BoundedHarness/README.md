# Demo330 — Bounded Harness

Enforce iteration, time and cancellation limits on an actual loop.

Prerequisites: [Demo060](../../demos/Demo060_ToolsCatalog/README.md), [Demo230](../../demos/Demo230_TypedWorkflow/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo330.App.csproj](Demo330.App.csproj) (App).

Execution mode: **deterministic**. No model for the documented default command. Optional live modes require the shared connection profile.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo330_BoundedHarness/Demo330.App.csproj -c Release --no-build
```

Observe: Enforce iteration, time and cancellation limits on an actual loop. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo340 — Loop Control](../Demo340_LoopControl/README.md).

## Scenario procedure


Run from BaseTechnique after building:

dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- complete
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- repeat
dotnet run --project demos/Demo330_BoundedHarness -c Release --no-build -- complete --live

See ../../docs/lot-010/ControlsAndHarness.md for all scenarios, capabilities, return codes and verification evidence.
