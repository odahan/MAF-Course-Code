# Backlog behavior tests

Run from BaseTechnique: `dotnet test tests/Maf.Backlog.Tests/Maf.Backlog.Tests.csproj -m:1 --configuration Release`.

Twenty-three deterministic cases exercise real MAF invocation and approval handling with a scripted IChatClient, real isolated SQLite files, invalid contracts, missing/unknown tools, refusals, errors, session and request binding, changed arguments, one-shot capabilities and absent lookup data. No provider is called and no model quality is inferred. Test data is supplied only as provider responses; expected actions are not fed into candidate prompts.
