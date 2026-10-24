# Lab07 — Reported usage, missing usage and estimates

Run the Release build of Lab07_OpenTelemetry from the canonical solution. It uses the common connection profile and reports one consumption record per actual model request, including the policy tool loop and the workflow reviewer.

1. Compare Input, Output, ReportedTotal and DerivedTotal. A provider total may differ from the input/output sum. Neither overwrites the other.
2. Run with --deterministic. No model is invoked; absence of usage is not a zero-token model request.
3. Inspect Consumption.Measure and its tests. Partial usage must preserve every missing counter as null.
4. Compare the character/4 context estimate with actual request usage. The estimate excludes instructions, schema, tools, question and provider formatting; it must never fill a missing provider counter.
5. Read UsageLedger. Complete totals require complete coverage. Known subtotals remain labelled as subtotals.
6. Supply a clearly fictitious TokenRate in a test. Without complete input/output counters and an explicit versioned rate, monetary cost is unknown. A local Ollama call is not proof of zero infrastructure cost.

Success: actual zero, missing usage, derived total, context estimate and estimated monetary cost remain distinguishable. No savings claim follows from a shorter context alone.
