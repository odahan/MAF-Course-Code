# Demo160 — Context Memory

Inject remembered facts through the native AI context provider.

Prerequisites: [Demo150](../../demos/Demo150_SharedFacts/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo160.App.csproj](Demo160.App.csproj) (App).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo160_ContextMemory/Demo160.App.csproj -c Release --no-build
```

Observe: Inject remembered facts through the native AI context provider. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo170 — Memory Drift](../Demo170_MemoryDrift/README.md).

## Scenario procedure


AIContextProvider : injection avant l'appel, extraction après succès des seuls messages User externes. Même admission stricte key=value, bornes et oubli coordonné que la variante manuelle.

Connexion commune : Ollama par défaut, API compatible OpenAI sur sélection explicite. Paramètres lus depuis la sortie, puis environnement et CLI. [Guide, commandes, exercices et limites](../../docs/lot-006/SessionsAndMemory.md).

Compiler avec la solution canonique et `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`. Toute commande dotnet build ou dotnet test doit inclure explicitement -m:1. Ligne vide ou EOF termine les consoles interactives ; les comparaisons EP013–EP015 s'exécutent puis terminent.

