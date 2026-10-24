# Lab03 solution — Host evidence validation

`SqliteVectorStore.Search` applies scope, active status, canonical source, ValidFrom, ValidUntil and product filters in SQL. It scores every eligible stored embedding before the hybrid top-K. `RetrievalPolicy.Rank` rejects cosine below 0.60 and zero meaningful lexical overlap, then combines dense and lexical ranks through RRF. The threshold is qualified only on the delivered corpus and model.

`EmbeddingIndex.RetrieveAsync` additionally joins an active product to its expected procedure before business ranking. Unknown products cannot authorize an answer. `RagAnswerService` refuses empty retrieval before creating a chat client, observes the provider/tool integration and uses a fresh stateless agent for each question.

To test a fabricated citation, construct a proposal with `CanAnswer=true` and `Evidence=[new Evidence("INVENTED#0", "A quotation that was never retrieved.")]`. Pass it with the current Retrieval to `AnswerGate.Validate`. It must return `Accepted=false` and empty Evidence. For an altered quote, use a real SourceId and change its text; the exact substring check must reject it.

The gate checks the current retrieved ID set, rechecks eligibility and validates literal quotations. It renders the final text itself from those quotations. This proves literal provenance; it does not automatically prove semantic relevance or completeness for every possible question.

`RagTests` contains executable examples for fabricated citations, altered quotations, noncanonical/future sources, transaction rollback, thirty ineligible closer vectors, source removal and shortened chunks. `RagAcceptance` records real embedding scores, MAF provider/tool calls, model refusals and one retrieved prompt-injection fixture. The acceptance script also reopens SQLite in a separate process.
