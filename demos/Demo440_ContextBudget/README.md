# Demo440 — Context Budget

Measure extraction, summary and compaction overhead separately.

Prerequisites: [Demo150](../../demos/Demo150_SharedFacts/README.md), [Demo420](../../demos/Demo420_SafeTelemetry/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo440.App.csproj](Demo440.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo440_ContextBudget/Demo440.App.csproj -c Release --no-build
```

Observe: Measure extraction, summary and compaction overhead separately. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo450 — Contract Regression](../Demo450_ContractRegression/README.md).

## Scenario procedure


EP015 : compare le corpus brut à un filtre de lignes propre à la fixture, puis rejoue les politiques EP013. Affiche caractères, estimation caractères/4 et usage fournisseur séparément. Ce filtre n'est pas un compresseur général.

Connexion commune : Ollama par défaut, API compatible OpenAI sur sélection explicite. Paramètres lus depuis la sortie, puis environnement et CLI. [Guide, commandes, exercices et limites](../../docs/lot-006/SessionsAndMemory.md).

Compiler avec la solution canonique et `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`. Toute commande dotnet build ou dotnet test doit inclure explicitement -m:1. Ligne vide ou EOF termine les consoles interactives ; les comparaisons EP013–EP015 s'exécutent puis terminent.

