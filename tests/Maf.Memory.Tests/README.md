# Tests mémoire et persistance

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Depuis la base technique : `dotnet test tests/Maf.Memory.Tests/Maf.Memory.Tests.csproj -m:1 -c Release`.

Tests temporels avec SQLite et MAF réels, client de réponses scripté observable : redémarrage, reset transactionnel, correction, bornes, provenance, invalidité atomique, admission explicite, oubli et mises à jour anciennes, absence de rétroaction des réponses, erreurs visibles, annulation, sérialisation/restauration, instruction incompatible et différence entre compaction de requête et réduction de l'historique sauvegardé. Les essais Ollama sont séparés dans tools/Verify-Lot006.ps1.

