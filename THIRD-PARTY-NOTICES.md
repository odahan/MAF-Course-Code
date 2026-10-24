# Third-party dependencies

Copyright and license notices for third-party packages remain with their owners.
The repository MIT license does not relicense dependencies, frameworks, models,
services or trademarks. NuGet restores package binaries and their license
metadata separately; restored package binaries are not included in this source repository.

The table lists direct centrally pinned packages, not every transitive package.
Each project packages.lock.json pins the complete resolved dependency graph.
Before redistributing compiled applications, inspect the restored packages and
carry forward the notices and license texts required by their upstream terms.
No assertion of a completed binary redistribution license audit is made here.

| Package | Pinned version | Upstream package/license metadata |
|---|---|---|
| Spectre.Console | 0.57.2 | [NuGet](https://www.nuget.org/packages/Spectre.Console/0.57.2) |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 10.0.13 | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.EnvironmentVariables/10.0.13) |
| Microsoft.Extensions.Configuration.CommandLine | 10.0.13 | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.CommandLine/10.0.13) |
| CommunityToolkit.Mvvm | 8.4.0 | [NuGet](https://www.nuget.org/packages/CommunityToolkit.Mvvm/8.4.0) |
| Microsoft.Agents.AI | 1.26.0 | [NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI/1.26.0) |
| Microsoft.Agents.AI.Harness | 1.26.0 | [NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI.Harness/1.26.0) |
| Microsoft.Agents.AI.Workflows | 1.26.0 | [NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI.Workflows/1.26.0) |
| Microsoft.Extensions.AI | 10.11.0 | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.AI/10.11.0) |
| Microsoft.Extensions.AI.OpenAI | 10.11.0 | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.AI.OpenAI/10.11.0) |
| OpenAI | 2.14.0 | [NuGet](https://www.nuget.org/packages/OpenAI/2.14.0) |
| Microsoft.Extensions.AI.Abstractions | 10.11.0 | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions/10.11.0) |
| OllamaSharp | 5.5.0 | [NuGet](https://www.nuget.org/packages/OllamaSharp/5.5.0) |
| ModelContextProtocol | 2.3.0 | [NuGet](https://www.nuget.org/packages/ModelContextProtocol/2.3.0) |
| Microsoft.Data.Sqlite | 10.0.13 | [NuGet](https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.13) |
| OpenTelemetry | 1.20.1 | [NuGet](https://www.nuget.org/packages/OpenTelemetry/1.20.1) |
| OpenTelemetry.Exporter.Console | 1.20.1 | [NuGet](https://www.nuget.org/packages/OpenTelemetry.Exporter.Console/1.20.1) |
| OpenTelemetry.Instrumentation.Http | 1.20.1 | [NuGet](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Http/1.20.1) |
| JsonSchema.Net | 9.4.1 | [NuGet](https://www.nuget.org/packages/JsonSchema.Net/9.4.1) |
| Polly.Core | 8.8.0 | [NuGet](https://www.nuget.org/packages/Polly.Core/8.8.0) |
| Microsoft.Extensions.Configuration.Json | 10.0.13 | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Json/10.0.13) |
| Microsoft.Extensions.Hosting | 10.0.13 | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Hosting/10.0.13) |
| Microsoft.Extensions.DependencyInjection | 10.0.13 | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/10.0.13) |
| Microsoft.Extensions.Logging.Console | 10.0.13 | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console/10.0.13) |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.13 | [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Options.ConfigurationExtensions/10.0.13) |
| MSTest.TestFramework | 4.2.3 | [NuGet](https://www.nuget.org/packages/MSTest.TestFramework/4.2.3) |
| MSTest.TestAdapter | 4.2.3 | [NuGet](https://www.nuget.org/packages/MSTest.TestAdapter/4.2.3) |
| Microsoft.NET.Test.Sdk | 18.3.0 | [NuGet](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.3.0) |

External runtime components include .NET/Windows Desktop, Ollama, and locally
selected chat/embedding models or cloud APIs. Obtain them separately and follow
their own terms. Do not bundle model weights or credentials in this repository.

The old files named docs/licenses/LICENCE.MD and LICENCE.EN.MD were private
licenses written by the course author, not third-party package licenses. This
repository replaces those contradictory notices with the scope of the selected
MIT grant. The originals are preserved outside this repository in the author's local archives.
