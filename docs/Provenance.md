# Provenance and stable identities

After Lot-016, the user requested [00-ConfigurateurDemo](../demos/00-ConfigurateurDemo/README.md), a new central setup utility. ProjectAdditions.json (unpublished local archive) registers its new project separately from the immutable 78-project migration inventory. The current solution contains 79 projects; Demo010–Demo490 are not renumbered. See the configurator receipt (unpublished local archive) for the shared runtime-configuration change and its checks.

The teaching order is defined by [the catalogue](DemoCatalog.md), independently of former course days, videos or episodes. Every one of the 78 original projects is retained. Autonomous planning, memory, approval and retrieval variants have separate identifiers because they have distinct entry points and observable comparisons. MCP and streaming clients/servers form two composite demos. Tests and shared libraries are classified separately.

Project path map (unpublished local archive) records all old/new project paths, origins, demo identifiers, roles and decisions. File path map (unpublished local archive) covers every migrated source and asset. Old executable names, namespaces and explicit AssemblyName values remain intentionally stable to preserve reflection, persisted identities, test discovery and launch contracts. Project filenames and solution display names use the new demo identifiers. No database schema, executor identifier, graph version, session or network contract was changed.

Lots 001–015 receipts, hashes, manifests and logs remain dated evidence. Their old paths must be resolved through these maps; they do not describe the current physical layout. Current launch procedures are the demo READMEs, CourseMap and updated operational guides. Historical nested solution files and filming notes are now preserved outside BaseTechnique in ReserveBaseTechnique/archived, with their former relative paths; BaseTechnique.slnx is canonical. Original trees, reference supports and baseline archives remain preserved.

## Original course relocation, 24 October 2026

The original course directories were moved without editing their contents:

| Former path relative to Cours | Current path relative to Cours |
|---|---|
| `J1/` | V1/J1 (unpublished local archive) |
| `J2/` | V1/J2 (unpublished local archive) |
| `docs/` | V1/docs (unpublished local archive) |

All **1,391 files** recorded for these three original directories in the historical preservation inventory still match their SHA-256 hashes after relocation. The relocation has no impact on solution projects, runtime configuration or assets: BaseTechnique uses its own shared code, configuration and teaching assets. This document's directory, `BaseTechnique/docs`, is separate from the original course's `V1/docs`.

`Cours/ReserveBaseTechnique/_Baselines/Lot-001-2026-10-23.zip` is a dated **322-file snapshot of the initial renovation import**, rather than an archive of the V1 directory. It includes 261 imported source files from the course, Les Jeudis and videos, 48 reference supports, and the initial documentation/tools/legal files. The 79 course source/assets files and four course documents mapped directly to V1 are byte-for-byte identical; the archive's own SHA-256 also matches its receipt. Preserve this historical snapshot separately from the original course. It is not a build or runtime dependency.

The `_Baselines` directory was moved from the Cours root into ReserveBaseTechnique on 24 October 2026. All three files were verified unchanged by SHA-256 after the move; see the relocation record (unpublished local archive). Absolute paths inside dated receipts still describe their original location.

The relocation verification (unpublished local archive) and 83-file comparison (unpublished local archive) record these checks. Historical receipts, path maps and the catalogue's `origins` fields retain their dated logical paths; they are provenance identifiers, not current launch paths. Resolve historical references to `Cours/J1`, `Cours/J2` and the original `Cours/docs` through the table above. Archived preservation scripts still assume the former layout and must be adapted before any future execution; prior receipts remain unchanged.

## Combined origins

- Demo020: `src/connection/Connection.Console`, `src/connection/Connection.Backend`.
- Demo400: `src/video4/ServeurMcpLocal`, `src/video4/ClientAgentMcp`.
- Demo410: `src/video5/ServeurAgentWpf`, `src/video5/Streaming.Client`, `src/video5/Streaming.Console`, `src/video5/ClientWebAgent`, `src/video5/ClientWpf`.
