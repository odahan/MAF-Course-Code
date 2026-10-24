# Durable approval tests

Tests exercise real SQLite rows and transactions, first-decision binding, invalid input, draft rebinding, concurrent duplicates and native MAF checkpoint restoration. The separate-process acceptance script complements these in-process tests: Verify-Lot009.ps1 (unpublished local archive).

```powershell
dotnet test tests/Maf.Durable.Tests/Maf.Durable.Tests.csproj -m:1 -c Release
```
