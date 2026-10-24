# Local MCP tools — Lot-011

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

## Prerequisites and architecture

.NET SDK 10.0.402, .NET 10, MAF 1.26.0 and ModelContextProtocol 2.3.0 use the existing central pins and lock files. No MCP service installation is needed. Only the optional live agent needs a running model provider. This lot renovates `demos/Demo400_LocalMcp`; original archives and reference slides/scripts remain preserved.

The host launches a trusted precompiled server via stdio, initializes MCP, lists tools including JSON input schemas, then either calls them directly or supplies the discovered `McpClientTool` objects to native `ChatClientAgent`. The native model requests the function; MCP sends the request to the separate server process; its result returns to the actual model conversation. Business implementations exist only in `SupportTools` on the server.

The exposed server catalogue is two read-only tools. The host selects an explicit allowlist before agent construction and checks names at invocation and direct-call boundaries. Discovery is capability metadata, not authorization. `ReadOnly` annotations and descriptions are advisory; they do not enforce access rights. The server publishes only fixture data. A real server with protected data would authenticate and authorize each request itself.

## Commands

From `BaseTechnique`, after the solution build:

```powershell
dotnet demos/Demo400_LocalMcp/Client/bin/Release/net10.0/ClientAgentMcp.dll
dotnet demos/Demo400_LocalMcp/Client/bin/Release/net10.0/ClientAgentMcp.dll --live
dotnet test tests/LocalMcp.Tests/LocalMcp.Tests.csproj -m:1 -c Release --no-build --no-restore
# Archived acceptance recipe: tools/Verify-Lot011.ps1 -Live
```

The default protocol demonstration needs no LLM and emits discovery schemas and three actual tool results. `--live` uses `Maf.DemoConnection` through the common `DemoSettings` loader and copied `ai-settings.json`. Environment keys such as `AI__LocalChat__Endpoint` and CLI options such as `--AI:LocalChat:Model=granite4:3b` override the profile. API selection must be explicit and uses the configured secret environment variable; no automatic cloud fallback occurs. The lot qualifies only the local Ollama profile.

The default sibling path follows the client's build configuration and is independent of the current working directory. `--server` accepts an absolute path to a trusted compiled server; quote paths containing spaces. The runtime never runs `dotnet run` or restores packages. Missing builds fail with exit 2 and a structured failure status; build the solution first.

## Contracts and evidence

| Request | Expected result |
|---|---|
| list_priority_customers | Three fixture customers |
| get_incident_status / INC-318 | Found, InProgress, Critical |
| get_incident_status / INC-999 | NotFound, no incident invented |
| Invalid/missing incidentId | MCP result isError=true |
| Host request for delete_incident | Refused before protocol dispatch |

Input is matched case-insensitively against the exact `INC-` plus three ASCII digits format; whitespace and trailing newlines are rejected. Unknown valid IDs are successful domain lookups with NotFound, not transport errors. Server messages and descriptions are in English. Application logs use stderr so stdout remains valid MCP traffic.

The direct protocol path records raw fixture result payloads. The native agent emits a separate invocation trace with tool name, fixture incident ID and success; its terminal status is independent of assistant prose. Completed requires all three demonstration lookups to have returned successfully. The MCP SDK serializes native AIFunction results as JsonElement; the middleware explicitly checks its isError field. Assistant text without tools and a partial lookup remain Incomplete. This completion rule is deliberately specific to the three-lookup exercise, even though the runner accepts a prompt parameter.

## Timeouts, cancellation and lifecycle

Initialization plus discovery share a 15-second deadline in the console. Each tool request has a 15-second deadline; the live run has a 120-second total deadline and a maximum of six admitted tool calls. The seventh call cancels the run and reports ToolBudget. Run timeout reports DeadlineExceeded. Caller cancellation propagates through MAF and MCP and is never converted into success; Ctrl+C returns 130. Other failed execution returns 2. There is no retry or provider fallback. Transport ownership and `await using` close the child server on success, failure or cancellation. Integration tests verify SDK completion reports a process exit after disposal.

Cancellation is cooperative and cannot undo an already completed effect. This sample exposes no mutations. A child program chosen with `--server` runs with host permissions; the allowlist is not an operating-system sandbox. Tool content is untrusted data, and the agent instructions reflect this; this lot does not establish adversarial injection resistance.

## Exercise and verification

1. Build the solution, launch the protocol demo and inspect the required incidentId schema.
2. Follow INC-318 across client request and server result; compare it with INC-999.
3. Run the integration tests to observe invalid input, denied tool, cancellation, deadline and process shutdown.
4. Run with --live and compare the actual invocation trace with the final prose. Remove a requested lookup in a deterministic fixture and observe Incomplete.
5. Add another read-only server tool. Observe that discovery can expose it while the host's selected catalogue excludes it until the host policy is deliberately updated.

`Verify-Lot011.ps1` runs from a different working directory, validates actual payloads and the missing-server failure, and optionally verifies all live tool calls. Its receipt points to separate process logs. `LocalMcp.Tests` uses real server subprocesses; only model responses are scripted. These tests establish transport and native orchestration behavior, not live model decision quality. The saved local-model run supplies the complementary live evidence. Existing controls, durable idempotence and RAG remain in their respective shared modules.
