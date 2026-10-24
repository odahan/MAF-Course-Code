# Demo130 — Session Persistence

Persist business conversation state and reload it across launches.

Prerequisites: [Demo040](../../demos/Demo040_FirstAgent/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo130.App.csproj](Demo130.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo130_SessionPersistence/Demo130.App.csproj -c Release --no-build
```

Observe: Persist business conversation state and reload it across launches. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo140 — Native Session](../Demo140_NativeSession/README.md).

## Scenario procedure


SQLite : contexte explicite durable, historique en processus. Commandes setctx, showctx, reset/cleardb. Initialisation sans suppression ; remplacement de la session après reset ou remplacement du contexte.

Connexion commune : Ollama par défaut, API compatible OpenAI sur sélection explicite. Paramètres lus depuis la sortie, puis environnement et CLI. [Guide, commandes, exercices et limites](../../docs/lot-006/SessionsAndMemory.md).

Compiler avec la solution canonique et `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`. Toute commande dotnet build ou dotnet test doit inclure explicitement -m:1. Ligne vide ou EOF termine les consoles interactives ; les comparaisons EP013–EP015 s'exécutent puis terminent.

