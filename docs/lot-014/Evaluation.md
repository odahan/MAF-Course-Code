# Reproducible evaluation and regressions

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

Current paths were updated for Lot-016. Use the [stable demo catalogue](../DemoCatalog.md) and [course map](../CourseMap.md); dated receipts retain their original paths.

This lot separates three kinds of evidence: application tests with controlled model doubles; explicit scripted fixtures of the evaluation pipeline; and observed candidate/judge outputs from a selected model connection. Passing fixtures never qualifies a model.

## Run the application regressions

From BaseTechnique, use PowerShell 7 and the pinned .NET SDK:

```powershell
dotnet build BaseTechnique.slnx -m:1 -c Release; dotnet test BaseTechnique.slnx -m:1 -c Release --no-build --no-restore
# Archived acceptance recipe: tools/Verify-Lot014.ps1 -SkipBuild
```

Build.ps1 restores locked dependencies and builds all projects before running deterministic tests. Verify-Lot014 with SkipBuild runs every test project in the solution and writes TRX results, then runs two fixture repetitions by default. Every build/test command uses -m:1. Local MCP/HTTP tests require permission to bind loopback ports. If using the workspace package cache, set NUGET_PACKAGES to the absolute BaseTechnique/.packages path and DOTNET_CLI_HOME to BaseTechnique/.dotnet-home. For an offline cached restore, NuGetAudit=false can be passed explicitly; this does not perform a vulnerability audit.

## Run observed evaluation

Ollama is the default. Install the common granite4:3b model before running:

```powershell
# Archived acceptance recipe: tools/Verify-Lot014.ps1 -Live -SkipBuild -Repeat 2
```

The live campaign calibrates the judge on eight authored positive references and eight negative counterexamples, then generates and evaluates all eight cases for each repetition. Every request creates a fresh MAF agent/session. The model clients are separate, but the default model and endpoint are shared. This is a dependent judge, not independent validation. A different already installed judge can be selected with -JudgeModel; the command line runner also accepts --judge-model.

For a direct prompt comparison, use new output directories:

```powershell
$dll = './demos/Demo460_CandidateEvaluation/bin/Release/net10.0/MafPromptJudgeDemo.dll'
dotnet $dll --mode live --prompt v1 --repeat 2 --output './docs/lot-014/runs/before'
dotnet $dll --mode live --prompt v2 --repeat 2 --output './docs/lot-014/runs/after'
# Archived acceptance recipe: tools/Compare-Evaluation.ps1 -Before './docs/lot-014/runs/before' -After './docs/lot-014/runs/after'
```

The runner returns 1 for a failed gate, including calibration, contracts, transport failures, cancellation or missing results. Retain the report on failure. Comparison reports status transitions by case and repetition; a pass becoming a failure returns 1. It rejects incompatible datasets, evaluator binaries, judge prompts, profiles and generation parameters. A comparison without regressions does not itself qualify a failing after-run: inspect ModelGatePassed too. Model labels alone cannot prove identical weights; retain the server version and model digests (ollama-profile.json in this lot's evidence).

Common configuration is loaded from ai-settings.json, environment AI__* overrides and AI:* command line arguments. The OpenAI-compatible API is available on explicit selection via AI:ChatProvider, AI:ApiChat:Model and its configured key environment variable. The judge model override uses the same selected protocol and endpoint. No API evaluation is claimed for this lot. ChatGPT account connections are excluded.

## Contracts and isolation

The candidate receives exactly CandidateInput: UserInput and Context. It receives no case ID, expected label, required/forbidden behavior, reference, counterexample, critical flag or expected action. System instructions are global policy, identical across cases. The model adapter takes strings, not an EvaluationCase; the evaluator constructs the public projection before calling it.

CandidateAnswer has Action (assist/refuse/clarify/abstain), nonempty Response up to 1200 characters, and an explicitly present Refused boolean. JudgeVerdict has two explicitly present boolean criteria and a nonempty Explanation. Unknown fields, missing/null required values and malformed JSON fail the contract. There is no output repair, expected-category substitution, score imputation or silent retry. The generated schemas use the same serializer options as strict host validation.

The host checks Action and Refused against each case; the judge checks actual prose against required and forbidden behavior. All checks must pass. Contract errors and execution errors retain their stage and raw output when available. Errors never count as successful refusals. The host must receive all expected case/repetition results for acceptance. Every failed case blocks the campaign; critical failures are also counted explicitly. There is no universal quality score or averaging gate.

The negative calibration answers deliberately preserve superficially correct Action/Refused flags. The judge must detect their harmful prose. Passing calibration is necessary for model qualification, but is not a guarantee of judge reliability.

## Traceability and interpretation

Each fresh run directory contains a report, SHA-256 manifest, and exact case/candidate/judge prompt snapshots. The manifest covers the runner and connection assemblies and distributed configuration. Reports identify suite and prompt versions, selected provider, endpoint and models, shared-model status, .NET/MAF versions, timestamp, parameters, repetitions, calibration outputs and every raw candidate/judge output. Secrets and transport exception messages are not copied into reports. The selected model digest and Ollama version for the acceptance campaign are recorded separately.

Raw model text can contain sensitive or injected content. The shipped corpus is synthetic. Real input requires a suitable retention/access policy for these local artifacts. These reports are ordinary local evidence, not tamper-proof records.

Reproducibility means replayable inputs/configuration and attributable observations; temperature zero does not promise byte-identical outputs. Small authored examples, a shared candidate/judge model, calibration on known examples, order/cache effects and two repetitions do not establish statistical reliability or production safety. No judge evaluates the application authorization boundary: regression tests cover those executable controls separately. The NO-EVIDENCE case evaluates answer behavior on a supplied context; SQLite retrieval quality remains covered by Maf.Rag.Tests and Lot-007 evidence.

