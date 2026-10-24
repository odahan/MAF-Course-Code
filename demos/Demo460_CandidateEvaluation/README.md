# Demo460 — Candidate Evaluation

Evaluate isolated candidate answers against versioned references and strict gates.

Prerequisites: [Demo190](../../demos/Demo190_RetrievedEvidence/README.md), [Demo450](../../demos/Demo450_ContractRegression/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo460.App.csproj](Demo460.App.csproj) (App).

Execution mode: **fixture**. No model for the documented default command. Optional live modes require the shared connection profile.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo460_CandidateEvaluation/Demo460.App.csproj -c Release --no-build -- --mode fixture
```

Observe: Evaluate isolated candidate answers against versioned references and strict gates. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo470 — Documented Approval](../Demo470_DocumentedApproval/README.md).

Read the [operational guide](../../docs/lot-014/Evaluation.md) for scenario arguments, additional modes and refusal cases.
