# Maf.Workflows

Shared typed backlog graphs for the Video2 progression. `BacklogWorkflow.Build()` creates the deterministic variant; supply `BacklogWorkflow.CreateReviewer(commonChatClient)` for a native MAF agent host. Build a fresh graph per independent input. The host retains the expected draft only for that run; checkpoint persistence and concurrent reuse are outside this example.

See the [guide](../../docs/lot-008/Workflows.md). No branch writes to SQLite or exposes a write tool. The agent response schema is supplemented with domain and exact fact validation on the host.
