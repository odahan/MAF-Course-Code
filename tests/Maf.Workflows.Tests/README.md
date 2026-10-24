# Workflow behavior tests

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Tests run real MAF graphs with a scripted `IChatClient` transport. They cover mutually exclusive branches, skipped agent calls, native binding and one model invocation, schema transmission, final domain/fact validation, provider errors, session separation and real workflow telemetry. No network/model is needed.

```powershell
dotnet test tests/Maf.Workflows.Tests/Maf.Workflows.Tests.csproj -m:1 --configuration Release
```

Real Ollama verification is separate: `tools/Verify-Lot008.ps1`.
