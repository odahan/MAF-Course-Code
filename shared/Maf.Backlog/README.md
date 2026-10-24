# Shared backlog business boundaries

BacklogDraft, Catalog, ToolOutcome, LookupResult, BacklogStore, WriteGate and ApprovalReview are shared by the renovated fundamental examples. Agent instructions and orchestration stay in each executable. DemoSettings loads the common Lot-003 connection profile.

See [the guide](../../docs/lot-005/ContractsAndTools.md) and [the tests](../../tests/Maf.Backlog.Tests/BacklogTests.cs). From BaseTechnique run `dotnet test tests/Maf.Backlog.Tests/Maf.Backlog.Tests.csproj -m:1 --configuration Release`.
