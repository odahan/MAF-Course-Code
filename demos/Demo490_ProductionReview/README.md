# Demo490 — Production Review

Review the remaining application-specific deployment decisions.

Prerequisites: [Demo470](../../demos/Demo470_DocumentedApproval/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo490.App.csproj](Demo490.App.csproj) (App).

Execution mode: **deterministic**. No model for the documented default command. Optional live modes require the shared connection profile.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo490_ProductionReview/Demo490.App.csproj -c Release --no-build
```

Observe: Review the remaining application-specific deployment decisions. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

End of the initial path; choose further application work from the extension guide.

## Scenario procedure


This entry prints review questions without a model call. It does not implement the listed controls. The [global guide](../../docs/lot-015/Guide.md), [audit matrix](../../docs/lot-015/AuditClosure.md) and integrated documented approval scenario provide the actual scoped guarantees and evidence. Historical filming notes remain provenance.
