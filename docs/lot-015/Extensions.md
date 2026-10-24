# Extension map and reference architecture

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

This map describes the scope of this distribution and possible later work. It is not a claim that an unimplemented feature exists or that every framework/provider supports it. The versions distributed here are pinned in `Directory.Packages.props`.

| Area | Demonstrated here | Possible extension and additional evidence needed |
|---|---|---|
| Deterministic orchestration | EP002 C# sequence/router and PipelineComplete | Choose code when the routing rule is known; compare complexity and failure behavior before adding an agent. |
| Native graphs | Video2 typed branches and native agent; Video3 request port/checkpoints | Concurrent fan-out/fan-in needs cancellation, joining and partial-failure tests. |
| Handoff / group conversation | Not implemented as a dedicated orchestration | Define ownership, message visibility, termination, tool rights and session boundaries; test loops and refusal. |
| Open-ended planning / Magentic-style coordination | Bounded Harness demonstrates a controlled agent loop, not a general multi-agent planner | Define budgets, allowed tools, stopping rules and external validation; evaluate the real workload. |
| Skills | No skill loader, skill discovery or executable skill sandbox is delivered | Treat skill text/assets as an extension input with provenance, admission rules, bounded access and host-controlled capabilities. A skill cannot grant itself tool permissions. |
| MCP / remote tools | Local stdio read-only server, schema discovery and selected tools | A remote deployment needs server-side authentication/authorization, transport protection and schema/version rollout tests. |
| A2A / hosted agents | No remote agent protocol or managed hosting deployment | Add identity, trust boundaries, timeouts, idempotent requests and operational receipts before claiming interoperability. |
| Providers | Shared Ollama/default and explicit API-compatible-by-key connection; dated capability receipts | Qualify each endpoint/model/protocol. Chat, embeddings and judge profiles are independent. |
| UI and hosting | Video5 ASP.NET Core backend; console, Web and WPF MVVM Toolkit consumers | GUI acceptance remains outstanding. Production hosting needs identity, tenant isolation, recovery, load and deployment checks. |
| Prompt profiles | EP014 versioned instructions independent of connection settings | Version prompt changes alongside references; rerun relevant evaluation rather than infer quality from a profile name. |
| Production review | EP018 deterministic checklist plus the [audit matrix](AuditClosure.md) | A checklist is a review aid; it does not implement any item or certify deployment readiness. |

The application architecture remains visible: an ASP.NET Core host owns configuration, dependency injection, cancellation and service lifetimes; shared connection factories supply transports; native MAF agents own instructions/tools/sessions; host validators and stores own effects. The clients depend on backend contracts rather than provider keys. Video5's clients stream through the shared client module and WPF commands/view-models use MVVM Toolkit. Consult [Streaming.md](../lot-012/Streaming.md) for exact launch commands and boundaries.

The integrated P14 console uses the same shared components, with an explicit durable decision boundary. It is intentionally not wired into the Video5 HTTP API. Safe phase traces and persisted business operation identities are different evidence from a transactionally linked journal. See [Scenario.md](Scenario.md) before designing a distributed deployment.
