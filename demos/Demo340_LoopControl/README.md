# Demo340 — Loop Control

Detect repeated requests and terminate a non-progressing agent loop.

Prerequisites: [Demo330](../../demos/Demo330_BoundedHarness/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo340.App.csproj](Demo340.App.csproj) (App).

Execution mode: **deterministic**. No model for the documented default command. Optional live modes require the shared connection profile.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo340_LoopControl/Demo340.App.csproj -c Release --no-build
```

Observe: Detect repeated requests and terminate a non-progressing agent loop. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo350 — Graceful Degradation](../Demo350_GracefulDegradation/README.md).

## Scenario procedure


The default now uses a native MAF ChatClientAgent and a scripted provider that requests the same typed read repeatedly. One body executes, then the scope reports RepeatedCall. Use `complete`, `changing` or `budget` to compare stopping policies; append `--live` to use the common Ollama configuration. See [the Lot-010 guide](../../docs/lot-010/ControlsAndHarness.md). Exit 2 represents an explicit policy stop.
