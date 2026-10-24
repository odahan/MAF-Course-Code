# 00 — Demo Configurator

Configure the common settings before starting the [49 learning demos](../../docs/DemoCatalog.md). This Spectre.Console utility appears first in the solution as **00-ConfigurateurDemo**. Its interface is English. It is a setup utility and does not renumber Demo010–Demo490.

From the **BaseTechnique root**:

```powershell
dotnet build BaseTechnique.slnx -m:1 -c Release
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build
```

For the menu, use a normal interactive terminal. You can also open the project from the canonical solution and run it. After the initial build, launching the existing executable works from any working directory:

```powershell
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build -- show
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build -- validate
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build -- models
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build -- check chat
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build -- check embeddings
```

## What the menu configures

- The active chat provider, Ollama URL/model, and OpenAI-compatible cloud URL/model.
- Independent embedding provider, URL, model, key-variable name and optional dimensions.
- Per-request timeout; absolute state/database/corpus paths; RAG eligibility date; backlog read-only host policy.
- Backend URLs for console/WPF and Web, streaming-session storage and the precompiled MCP server path.
- Instruction profile and the three optional Ollama routing-model overrides.

The menu shows stored values beside effective saved values, including environment overrides. API key values are never displayed. View/model-list commands do not write configuration or install models. Explicit chat/embedding checks send a real request using the saved selected profile; a cloud profile uses its configured API service. They do not qualify model quality, tool calling or structured-output capabilities. Use Demo030 for capability qualification.

## Central file and precedence

The default editable file is `shared/Configuration/ai-settings.json`. At startup, demos find it by walking up from their executable to the canonical solution. They do not depend on the working directory. Existing binary-local copies provide fallback defaults when no shared tree is available.

Priority, from lowest to highest: **binary defaults → central file → environment → demo command-line arguments**. Saving changes applies to newly started programs **without rebuilding**. Restart an already running demo or backend. Changing the chat profile leaves embeddings independent; changing the embedding provider/model/dimensions requires rebuilding the RAG index explicitly. Existing business/session data is never deleted by the configurator.

For a standalone layout or an isolated profile, set `MAF_DEMO_CONFIG` to an existing absolute JSON path. `--config <file>` selects an alternative file for the configurator only; set the environment variable as well when launching demos against it.

```powershell
$env:MAF_DEMO_CONFIG = 'E:/MyProfiles/ai-settings.json'
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build
```

Saves validate the stored profiles, preserve unrelated JSON sections and create a uniquely named backup beside the file. A stale editor session refuses to overwrite a file saved by another session. Invalid edits leave the previous file intact. Configuration backups and edit locks are excluded from source packaging.

## API keys

The JSON stores only environment variable names, by default `MAF_API_KEY` for chat and `MAF_EMBEDDING_API_KEY` for embeddings. The interactive **Store API key** action uses masked input and saves the value in the **Windows user environment**, separate from JSON and backups. To avoid overwriting ordinary system variables, this action accepts names beginning with `MAF_` or ending in `_API_KEY`. Existing process environment values take precedence over user values. Alternatively inject keys externally; the configurator does not require keys to edit or validate profiles. There is no ChatGPT account login.

## Scripted edits

```powershell
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build -- set AI:LocalChat:Model granite4:3b
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build -- set AI:ApiChat:Model your-cloud-model
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build -- set AI:ChatProvider OpenAICompatible
```

Set the cloud model before selecting the cloud provider. `set` accepts only documented non-secret fields; it validates and saves with a backup. An empty value clears an optional override. Operational choices such as starting/stopping servers, installing models, selecting an evaluation case, choosing approval decisions, per-run ports (`--urls`) and overriding CORS (`--Cors:Origins:0`) remain explicit demo actions or arguments. They are not executed automatically by editing settings.

Verification: [configuration tests](../../tests/Maf.DemoConnection.Tests/ConfigurationTests.cs) and configurator acceptance (unpublished local archive).
