# V100 — First published technical base

Published version: **V100**, 24 October 2026. Repository: [odahan/MAF-Course-Code](https://github.com/odahan/MAF-Course-Code).

This version publishes the cleaned BaseTechnique source tree at the repository root. Open [BaseTechnique.slnx](../BaseTechnique.slnx) and follow the [root README](../README.md) for the locked restore, build, test and launch commands.

## Included

- 49 numbered learning demos, with stable identifiers Demo010–Demo490.
- The central `00-ConfigurateurDemo` setup utility.
- Shared configuration, libraries, runtime assets and teaching exercises.
- 79 solution projects, including 13 automated test suites.
- The course map, demo catalogue, technical guides, provenance and legal notices.

The SDK is pinned to .NET 10.0.402. WPF requires Windows. The default local chat provider is Ollama; embeddings have their own profile. Vector retrieval uses Microsoft.Data.Sqlite and real dense-vector cosine search in C#. No Qdrant or Docker is required.

## Validation

On 24 October 2026, the cleaned source tree was restored with its locked dependencies and rebuilt after removing all previous build output: **79 projects, zero warnings and zero errors**. All **241 tests across 13 suites passed**, with no failures or skipped tests. Local MCP and streaming integration tests are included in those suites. Tests require local interprocess and loopback communications; the successful run used an unrestricted local test host after sandboxed test startup stalled.

Documentation-only publication changes replace links to private local archives with explicit historical references. All repository-relative Markdown links and solution/project asset paths were checked before publication. No application source, runtime configuration, package version or test was changed for publication. Build output, package caches, local data, backup files and intermediate reports are excluded.

Historical acceptance campaigns and migration reports remain in the author's local reserve. They are not part of this repository. Older script names and former source paths in technical guides describe that renovation history; use the current demo READMEs for launch commands and the root README for build/test commands.

## Limits and rights

Passing application tests does not qualify a model. The default granite4:3b candidate/judge profile remains unqualified. Graphical Web/WPF acceptance, independent domain-reference sign-off and production qualification remain unperformed. Live providers and models require explicit configuration and separate evaluation; cloud usage can incur provider charges.

Publication does not change usage rights. Read [AVIS-LEGAL.txt](../AVIS-LEGAL.txt) and the preserved [upstream licence notices](licenses/LICENCE.EN.MD). No open-source licence is granted by this release.
