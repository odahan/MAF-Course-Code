# Demo380 — Role Based Tools

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Filter exposed tools and enforce authorization at execution time.

Prerequisites: [Demo060](../../demos/Demo060_ToolsCatalog/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo380.App.csproj](Demo380.App.csproj) (App).

Execution mode: **deterministic**. Scripted transport by default; --live explicitly selects the shared model connection.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo380_RoleBasedTools/Demo380.App.csproj -c Release --no-build
```

Observe: Filter exposed tools and enforce authorization at execution time. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: [build instructions](../../README.md); the acceptance record (unpublished local archive) distinguishes deterministic checks from dated live campaigns. Shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo390 — Prompt Profiles](../Demo390_PromptProfiles/README.md).

Read the [operational guide](../../docs/lot-005/ContractsAndTools.md) for scenario arguments, additional modes and refusal cases.

The default host-forbidden scenario exits 2. This is the expected refusal, not a successful tool execution.
