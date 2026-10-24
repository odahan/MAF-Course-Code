# Demo410 — Streaming Interfaces

Use a shared streaming backend from console, Web and WPF clients with persistent sessions.

Prerequisites: [Demo130](../../demos/Demo130_SessionPersistence/README.md), [Demo400](../../demos/Demo400_LocalMcp/README.md). See the [course map](../../docs/CourseMap.md).

Projects: [Demo410.Backend.csproj](Backend/Demo410.Backend.csproj) (Backend); [Demo410.Client.csproj](Client/Demo410.Client.csproj) (Client); [Demo410.Console.csproj](Console/Demo410.Console.csproj) (Console); [Demo410.Web.csproj](Web/Demo410.Web.csproj) (Web); [Demo410.Wpf.csproj](Wpf/Demo410.Wpf.csproj) (Wpf).

Execution mode: **real**. Shared ai-settings.json; Ollama granite4:3b by default, explicit API key profile optional. RAG additionally requires independent embeddinggemma:300m embeddings.

Open PowerShell at the **BaseTechnique root**, restore/build with `dotnet build BaseTechnique.slnx -m:1 -c Release` first, then run:

```powershell
# Start the backend, then run the console from a second shell.
dotnet run --project demos/Demo410_StreamingInterfaces/Backend/Demo410.Backend.csproj -c Release --no-build -- --urls http://localhost:5088 --Conversation:StateRoot (Join-Path $env:TEMP ('maf-demo410-' + [guid]::NewGuid().ToString('N')))
$env:MAF_BACKEND_URL = 'http://localhost:5088'
dotnet run --project demos/Demo410_StreamingInterfaces/Console/Demo410.Console.csproj -c Release --no-build -- 0123456789abcdef0123456789abcdef '/incident INC-318'
# Optional browser host and WPF client:
dotnet run --project demos/Demo410_StreamingInterfaces/Web/Demo410.Web.csproj -c Release --no-build
dotnet run --project demos/Demo410_StreamingInterfaces/Wpf/Demo410.Wpf.csproj -c Release --no-build
```

Observe: Use a shared streaming backend from console, Web and WPF clients with persistent sessions. Inspect actual outputs and persisted state before proceeding. Validation errors, denied writes, invalid evidence and unavailable dependencies must remain explicit; never interpret an acknowledgement as a business effect.

Verification: shared behavior tests run with `dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore`.

Limits: Teaching scenario only. Existing host controls and persistence formats are preserved. Default granite4:3b evaluation remains unqualified; no production or independent human-domain qualification. Graphical Web/WPF acceptance is still unperformed.

Suggested next step: [Demo420 — Safe Telemetry](../Demo420_SafeTelemetry/README.md).

Read the [operational guide](../../docs/lot-012/Streaming.md) for scenario arguments, additional modes and refusal cases.

