# Demo020 — Connection Profiles

Select and diagnose independent chat and embedding connections, from console or HTTP.

Prerequisites: [Demo010](../../demos/Demo010_CodeOrAgent/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo020.Console.csproj](Console/Demo020.Console.csproj) (Console); [Demo020.Backend.csproj](Backend/Demo020.Backend.csproj) (Backend).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
dotnet run --project demos/Demo020_ConnectionProfiles/Console/Demo020.Console.csproj -c Release --no-build
dotnet run --project demos/Demo020_ConnectionProfiles/Backend/Demo020.Backend.csproj -c Release --no-build
```

Observe: Select and diagnose independent chat and embedding connections, from console or HTTP. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification.

Suggested next step: [Demo030 — Agent Capabilities](../Demo030_AgentCapabilities/README.md).

Read the [operational guide](../../docs/lot-003/Connection.md) for scenario arguments, additional modes and refusal cases.
