# SQLite RAG module

This module stores real `embeddinggemma:300m` embeddings in Microsoft.Data.Sqlite, calculates exact cosine similarity in C#, applies governed SQL filters before ranking, and fuses dense and lexical ranks. It requires no vector service or SQLite extension.

`RagDemo` provides the five console entry points. `SqliteVectorStore` owns transactional persistence; `EmbeddingIndex` owns the common embedding client; `RagAnswerService` owns stateless MAF integration; `AnswerGate` validates literal quotations from the current retrieved excerpts.

See [the complete guide](../../docs/lot-007/CorpusAndRetrieval.md) for commands, schema, corpus governance, calibration, limitations and acceptance evidence.
