# 0001 — Catalog ingestion: its own tool, its own file, one chunked table

**Status:** accepted
**Date:** 2026-09-21
**Deciders:** product owner, engineer

## Context

`CoreRentalNet.CatalogIngestion` builds the vector table the workspace suggester reads. The product
owner redefined the change, and the redefinition narrows rather than widens it:

- **The tool stands on its own.** It reads `products.json` through the catalogue module's published
  `ProductCatalog` and writes a file of its own. The retrieval module (`Discovery`) is **not** touched
  by this work.
- **Ingestion embeds through a local server** speaking the OpenAI protocol, not through Azure.
- **Each product becomes many vectors**, because its text is split into chunks before it is embedded.
- **All of it lives in one table**, in the tool's own SQLite file.
- **Every parameter is configuration**, defaulted to the values this tool is known to work with.

Measured against the running server before the work: it serves `all-MiniLM-L6-v2-embedding`, returns
**384** floats per text, and normalises them to unit length.

Constraints that shape the record:

- **ARC-05** forbids a project from pinning package versions itself; every version lives in
  `Directory.Packages.props`.
- The tool must be **reproducible**: the same catalogue and settings produce the same vectors.
- **Nothing else in the solution may change.**

## Decision

**D1 — The tool owns its pipeline and its own persistence.** It references the catalogue loader
(`Modules.Catalog.Infrastructure`) and the shared SQLite plumbing (`BuildingBlocks.Infrastructure`),
and **no** `Modules.Discovery` project. It creates and writes its own database file; nothing else reads
it yet.

**D2 — Embeddings come from the local OpenAI-compatible server.** `Llm:Server` and `Llm:Model` name it;
the model is `all-MiniLM-L6-v2-embedding` and the width is **384**. The width is **verified, never
requested**: the server ignores the OpenAI `dimensions` option, so a vector of another width is refused
rather than stored.

**D3 — Each product is split with `SemanticChunker.NET`.** All seven of its parameters are configuration
with defaults. The text it splits is `name + description + the whole metadata`, rendered
deterministically — attributes ordered by key, bare newlines, empty sections omitted.

**D4 — One table holds everything.** `product_embedding`, with columns
`id, skuNo, name, description, embedding, created_at`. Every chunk of a product repeats that product's
`name` and `description`; `id` is a new GUID per row; `created_at` is the run's UTC timestamp, the same
on every row of one run; `embedding` is 384 little-endian floats (1,536 bytes). There is deliberately
**no recipe table**.

**D5 — Raw `Microsoft.Data.Sqlite`, replacing the whole table in one transaction.** No ORM: the table is
one shape, written whole and never queried by the tool.

**D6 — Configuration is a file, with defaults.** `appsettings.json` beside the tool, overridable by a
gitignored `appsettings.Local.json`. **An absent key takes its default; a key that is present but
unusable stops the run.** They are not the same, and nothing else records what a run used.

**D7 — Nothing is written until every vector is in hand.** An empty catalogue, a product that yields no
chunks, or a vector of the wrong width refuses the whole run before the table is emptied.

**D8 — There is no search.** The tool ingests only. Ranking and the application's use of the table belong
to the retrieval module, which this work leaves exactly as it found it.

## Consequences

**Good**

- The retrieval module is untouched, so nothing it proves is put at risk by this change.
- Ingestion is one deliberate step: one command, one file, one table, configurable without a rebuild.
- The width is verified against the server, so a misconfigured model fails loudly instead of storing
  incomparable vectors.
- Measured: 205 products became 638 rows in 25 s against the local server.

**Bad**

- **Nothing records what built the vectors.** With no recipe table, a changed model, width or chunker is
  invisible; the configuration file is the only statement of it, and nothing compares the two.
- **Nothing reads the table yet**, so this work can be judged only on the rows it writes, not on
  retrieval quality.
- **The tool depends on the solution's catalogue module**, so it is not a free-standing program.
- **Reproducibility rests on the server and the chunker behaving identically**, and neither is pinned by
  anything the tool stores — only by its package version and its configuration.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Keep the pre-existing Foundry-and-CLI ingestion | Ingestion was redefined to run locally, without a cloud credential |
| Reference `Modules.Discovery` for the embedding port and the vector store | The retrieval module was to be left alone; a standalone tool touches it in no way |
| Let the tool pin its own package versions | **ARC-05** forbids it — versions live in `Directory.Packages.props` |
| A second, "recipe" table recording model, width, chunker and catalogue hash | The product owner chose one table; the loss of a staleness check is recorded above |
| EF Core over the raw provider | One table, written whole and never queried; a mapping layer would be machinery with nothing to map |
| A search or ranking path inside the tool | Explicitly out of scope; the retrieval module owns retrieval |
| Silent fallback when a configured value is unusable | Substituting a value the operator did not choose leaves no trace, and nothing records what a run used |

## Notes

- The model id and width were **verified against the live server**, not assumed: `all-MiniLM-L6-v2-embedding`
  answers with 384 unit-length floats. The name in the earlier design, `all-MiniLM-L6-v2`, does not
  resolve on that server.
- `SemanticChunker.NET` brings `ICU4N`, whose build target raises `ICU4N_IDE_0002`. It is demoted in the
  tool's own `.csproj`, with the reason written beside it, and again in the test project's, because the
  target travels with the package to every project that references the tool.
- The tool's test project was rebuilt from the pre-change suite: its command-line settings tests are gone,
  and its 38 tests now cover the chain (catalogue → chunk → embedding → stored row), the store and its
  nearest-row ranking, the renderer, the splitter and the vector codec, alongside the configuration.
- **D4 was reversed by [0003](0003-discovery-reads-the-tool.md).** There is now a recipe table,
  `product_embedding_recipe`, because the application must decide whether it can search the vectors at all and
  a recipe kept apart from the rows it describes is one that can lie about them. The cost recorded here — no
  staleness check — is what that buys back.
