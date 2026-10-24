# Lab03 exercise — Validate evidence in SQLite RAG

> Historical acceptance scripts and reports are kept in the author's local reserve and are not included in this repository. Use the root README for build/test commands and the demo READMEs for current launch commands.

The starting point is the renovated Lab03_RagAgent or Lab03B_RagProvider. Both use Maf.Rag, the same SQLite database and the same relevance policy. This is a focused modification exercise, not an incomplete Start/End project pair.

1. Rebuild the index with embeddinggemma:300m. Run `probe` for a relevant support question and an unrelated cooking question. Explain the difference between cosine and RRF rank.
2. Call `AnswerGate.Validate` with a source ID that was never retrieved. Check that the host refuses it and exposes no generated evidence.
3. Keep a real source ID, but alter one word in its quotation. Check that the host also refuses this proposal.
4. Set a source validity start to tomorrow, rebuild, and supply an explicit `Rag:AsOf`. Verify that the source cannot displace an admissible result before top-K.
5. Remove a document or shorten it to fewer chunks, rebuild, reopen SQLite and check that old excerpts are absent.

Acceptance: IDs alone are insufficient. Accepted output consists only of literal passages from sources retrieved for the current question and eligible at the supplied date. Empty evidence refuses before chat. A prompt instruction or a visible SOURCES heading does not prove these invariants.

Run the deterministic tests with `dotnet test tests/Maf.Rag.Tests/Maf.Rag.Tests.csproj -m:1 --configuration Release` from BaseTechnique. Run `./tools/Verify-Lot007.ps1` for real embeddings and MAF integration. See `docs/lot-007/CorpusAndRetrieval.md` for the measured threshold and its limits.
