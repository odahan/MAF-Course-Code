# SQLite RAG tests

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

The deterministic suite uses actual temporary SQLite files and controlled vectors. It covers cosine geometry, persistence, full replacement, filters before top-K, validity boundaries, index incompatibilities, transaction rollback, corrupt vectors, corpus identity, structured facts, hybrid relevance, and host citation gates.

Run from the canonical root:

```powershell
dotnet test tests/Maf.Rag.Tests/Maf.Rag.Tests.csproj -m:1 --configuration Release
```

For real Ollama embeddings and MAF integrations, build the solution and run `./tools/Verify-Lot007.ps1`. The real acceptance matrix is deliberately separate from the deterministic test suite.
