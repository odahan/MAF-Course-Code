# Lab07 — Native agent, workflow, tool and HTTP correlation

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

1. Run tools/Verify-Lot013.ps1 -Live from the canonical base after building it.
2. Identify host, maf.agent, maf.workflow and http sources. Confirm they share one trace ID and retain span and parent-span IDs.
3. Distinguish Workflow event notifications from Trace records and durable Journal rows. A console workflow event is not a business transaction.
4. Inspect the actual get_review_policy invocation. It is a read acknowledged by the host. Only the host SQLite transaction emits ActionCommitted.
5. Repeat with --fail. The workflow completes, the local write tool fails, the root trace ends in Error, and no action row is committed.
6. Reopen the database in another process using --db PATH --read EXECUTION_ID. Verify the journal action identifier against the local business table.
7. Review the hostile-payload test: raw prompts, answers, URLs, events and exception messages must not appear in the safe export.

Success: a native MAF agent and workflow, actual HTTP requests and tool execution can be followed across one trace; failure cannot become a fallback success or an invented action.
