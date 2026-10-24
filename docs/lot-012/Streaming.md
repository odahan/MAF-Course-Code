# Shared backend and streaming interfaces

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

## Start the demo

From `BaseTechnique`, build the canonical solution first:

```powershell
dotnet restore BaseTechnique.slnx -m:1 --locked-mode --disable-parallel --packages .packages -p:NuGetAudit=false
dotnet build BaseTechnique.slnx -m:1 -c Release --no-restore -p:UseSharedCompilation=false
```

Start each selected application in its own terminal:

```powershell
dotnet run --project demos/Demo410_StreamingInterfaces/Backend -c Release --no-build --no-launch-profile
dotnet run --project demos/Demo410_StreamingInterfaces/Web -c Release --no-build --no-launch-profile
dotnet run --project demos/Demo410_StreamingInterfaces/Wpf -c Release --no-build
```

Open http://localhost:5089 for the browser client. The WPF client requires Windows. The backend listens on http://localhost:5088 by default; both launch profiles use matching HTTP ports. The web shell serves no model endpoints. It reads `Backend:Url` for its target URL. Native clients read `MAF_BACKEND_URL`.

The console also uses the same HTTP client as WPF. It prints its session ID; reuse it for a second turn or after a backend restart:

```powershell
dotnet run --project demos/Demo410_StreamingInterfaces/Console -c Release --no-build -- 0123456789abcdef0123456789abcdef "/incident INC-318"
dotnet run --project demos/Demo410_StreamingInterfaces/Console -c Release --no-build -- 0123456789abcdef0123456789abcdef "Which customer did we just discuss?"
```

The backend copies and loads the common `shared/Configuration/ai-settings.json` from its output directory. Environment variables and command-line arguments override the file. Ollama `granite4:3b` is the default. Select API mode explicitly with `AI__ChatProvider=OpenAICompatible`, configure `AI__ApiChat__Model` and the API key environment variable described in the common connection guide. The clients never receive a provider key. There is no ChatGPT account login.

## Wire contract

Both POST endpoints take `{ "sessionId": "32 hex digits", "message": "user text" }`. The server validates the ID, rejects blank or over-4096-character messages and caps the request body at 32 KiB. No client-supplied display history is accepted as model state.

`POST /discussion` buffers the same streaming generation and returns `{ "message": "complete text" }` only after success and persistence. `POST /discussion/flux` returns UTF-8 `application/x-ndjson` events, one JSON value per line:

```json
{"type":"text","text":"Hello"}
{"type":"text","text":" "}
{"type":"text","text":"world\n"}
{"type":"completed","text":null}
```

Empty model text is ignored; spaces, tabs, line breaks and Unicode are retained exactly. JSON escaping keeps embedded newlines distinct from event delimiters. HTTP packets, stream reads, model updates and tokens have different boundaries. The .NET reader buffers lines rather than reading a character at a time. The browser uses a streaming UTF-8 decoder, keeps incomplete lines, flushes the decoder at EOF, and updates only the current answer DOM node with `textContent`.

Only `completed` means generation and snapshot saving succeeded. A failure before headers produces HTTP 502 (409 for the single-process generation gate, 400 for input validation). A failure after text begins produces an `error` frame and no `completed` event. Public error text excludes exception details. EOF without completion is a truncated response, never a success. This teaching protocol does not expose token usage, hidden reasoning, tool-call deltas or human approval events. The MCP lookup occurs explicitly before model generation.

## State and cancellation

The server uses the Lot-006 `SessionFile` envelope, native MAF sessions, versioned instructions (`video5-1`) and the bounded history policy. IDs are normalized before resolving file names. The default root is `MAF_STATE_ROOT/video5`, or the Lot-006 LocalApplicationData location when unset; `Conversation:StateRoot` can override the demo directory. Successful turns are saved by atomic neighbor-file replacement. A fresh session is restored for each run, so canceled and failed model mutations never remain in an in-memory session cache. Invalid snapshots fail visibly and are not silently reset.

The browser and WPF histories are presentation only. Reusing an ID resumes the bounded model history even after server restart but does not repopulate the display. WPF has an editable session ID; the browser retains its ID in session storage. New conversation creates a new ID and clears the display. Old snapshots remain until explicitly removed offline; no forget-all promise is made.

WPF uses MVVM Toolkit observable properties and an asynchronous command with a generated cancel command. It appends fragments to the existing observable answer. The browser uses AbortController and disables competing actions during a request. The console uses Ctrl+C. Cancellation passes through HTTP reads, ASP.NET RequestAborted, MAF streaming, MCP initialization/calls and session saving. The server bounds a run to 120 seconds. One generation at a time is allowed across the demo process; competing requests receive 409 rather than waiting without bounds.

Cancellation observed before commit leaves the last successful snapshot unchanged. Cancellation after commit cannot undo that commit. A disconnect can lose the completion acknowledgement after saving, so clients report **completion unconfirmed**, retain partial text for inspection, and do not automatically retry. The demonstration has no request deduplication or exactly-once turn protocol. Backend restarts and competing backend processes must not write the same state directory concurrently.

## Local exposure and MCP integration

The default backend binds to loopback, rejects non-loopback peers and allows the exact browser origin `http://localhost:5089` for POST with Content-Type. Override `Cors:Origins` only for a deliberate local setup. CORS is a browser restriction; it neither authenticates WPF nor protects sessions. This local demo has no user identities, access controls or remote deployment contract. Session IDs are routing identifiers, not authentication tokens. Loopback binding and trusted local users are part of the scenario.

Send `/incident INC-318` or `/incident INC-999` from any client. The backend starts the **precompiled** Lot-011 MCP server, discovers its tools and explicitly calls the host-allowed `get_incident_status` tool, checking `IsError`. It supplies the actual result as untrusted context to the streaming MAF agent. This is a host-routed read-only integration, not an autonomous tool-selection demonstration. Rebuilding the canonical solution builds the sibling MCP server. `Mcp:ServerPath` can supply an explicit compiled DLL. Each lookup owns and disposes its stdio subprocess. No business mutation, RAG service or Docker is introduced.

## Exercise

Send a marker in one successful turn, clear only the client display, and ask about the marker with the same ID. Repeat with a new ID and observe isolation. Then start a long answer, cancel it, and inspect the saved session: the canceled turn must not replace the previous snapshot when cancellation reaches the server before commit. Compare complete and streaming endpoints with the controlled whitespace tests; do not assess whitespace fidelity from a model paraphrase.
