# Microsoft Agent Framework teaching demos

Version **V100** is the first published technical base. See the [release notes](docs/ReleaseNotes.md) for its scope, validation and limits. The [GitHub repository](https://github.com/odahan/MAF-Course-Code) contains this source tree at its root.

## Complete course

The complete Microsoft Agent Framework course will be available at [e-naxos.com/maf](https://e-naxos.com/maf). This repository provides the technical demos and exercises that accompany the course.

The course page is not live yet and will open soon. In the meantime, visit [e-naxos.com](https://e-naxos.com) for further information.

Start with [00-ConfigurateurDemo](demos/00-ConfigurateurDemo/README.md) to configure local or cloud chat, independent embeddings and shared runtime settings. Newly started demos read saved configuration without rebuilding. API keys stay outside JSON.

The canonical solution is [BaseTechnique.slnx](BaseTechnique.slnx). It contains **79 projects**: 49 numbered learning demos, the central configurator, shared libraries, tests and exercises. Use the [demo catalogue](docs/DemoCatalog.md), [course map](docs/CourseMap.md) and [technical guide](docs/lot-015/Guide.md) to choose a starting point.

## Build and test

Use Windows, PowerShell 7 and the .NET SDK pinned in `global.json` (**10.0.402**). Windows is required for the WPF project. Initial package restore needs NuGet connectivity. Run these commands from this directory:

```powershell
dotnet restore BaseTechnique.slnx -m:1 --configfile NuGet.Config --locked-mode --disable-parallel
dotnet build BaseTechnique.slnx -m:1 -c Release --no-restore
dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore
dotnet run --project demos/00-ConfigurateurDemo -c Release --no-build
```

Every `dotnet build` and `dotnet test` command must explicitly include `-m:1`. Build outputs and restore intermediates are regenerated as needed. No Node.js runtime or renovation script is required.

For an offline machine, point `NUGET_PACKAGES` at an already populated cache and replace the restore command with:

```powershell
dotnet restore BaseTechnique.slnx -m:1 --configfile NuGet.Config --locked-mode --disable-parallel --ignore-failed-sources -p:NuGetAudit=false
```

Missing packages still fail restoration. This offline command does not perform an online vulnerability audit.

## Run the demos

Follow each demo's README for its arguments, expected results and model requirements. After building, the general launch command is:

```powershell
dotnet run --project demos/Demo010_CodeOrAgent/Demo010.App.csproj -c Release --no-build
```

Ollama is the default local provider. Live examples require the configured models; cloud providers require explicit endpoint/model settings and an environment-held API key. Chat and embedding profiles are independent. RAG uses **Microsoft.Data.Sqlite**, real dense vectors and exact C# cosine search. No Qdrant or Docker is required.

[Demo470](demos/Demo470_DocumentedApproval/README.md) connects retrieved evidence to durable approval. The [Actionability Start/End exercise](exercises/Actionability/README.md) remains separate. The default granite4:3b candidate/judge profile is unqualified; graphical Web/WPF acceptance, independent domain-reference sign-off and production qualification remain unperformed.

## Directory contents

- `demos`: the central configurator and the 49 learning demos, with their launch instructions and runtime assets.
- `shared`: common libraries, training code, configuration and assets required by the projects.
- `tests`: the 13 automated test suites referenced by the solution.
- `exercises`: teaching exercises and the Actionability projects.
- `docs`: the catalogue, course map, useful technical guides, provenance and upstream licence notices.

Historical reports, renovation scripts, old nested solutions, filming notes, distribution archives, reference supports and local caches are retained in the author's local `ReserveBaseTechnique` directory and are not published here. They are not needed to compile or run the demos. Older script recipes mentioned in technical guides are historical references. Use the direct build/test commands above for the current source tree.

Read [provenance](docs/Provenance.md), [upstream licences](docs/licenses/LICENCE.EN.MD) and [AVIS-LEGAL.txt](AVIS-LEGAL.txt). Original course trees and archives remain preserved.
