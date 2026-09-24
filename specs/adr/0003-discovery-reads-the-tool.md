# 0003 — The application reads the vectors the ingestion tool wrote

**Status:** accepted
**Date:** 2026-09-21
**Deciders:** product owner, engineer

## Context

[0001](0001-catalog-ingestion.md) and [0002](0002-ingestion-tool-internals.md) made ingestion a standalone
tool: its own file, its own table, a local OpenAI-compatible server, MiniLM at **384**, and deliberately **no
recipe table**. The retrieval module was left exactly as it was, on purpose, to be adjusted later. This is that
adjustment.

Reading `Modules/Discovery` against the tool found six things that did not line up:

1. **The vectors were not comparable.** The application embedded each customer's request with Foundry's
   `text-embedding-3-large` at **512**; the tool wrote MiniLM vectors at 384. Cosine similarity between those
   is not a worse answer, it is a meaningless one.
2. **Nothing read the tool's file.** The shortlist read `Discovery_CatalogVector` out of the application's own
   database, so the tool's output — 638 rows — was inert.
3. **Granularity differed.** The application stored one vector per product, keyed by SKU; the tool stores one
   per passage, so a product has many.
4. **The freshness gate had nothing to read.** It compared a `Discovery_Index` row that only the application's
   own ingestion ever wrote. Only `CatalogIngestion.ReplaceAsync` produced it, and that class was reachable
   **only from tests** — `ICatalogIngestion` was never registered and never called.
5. **The freshness check was not registered either.** `DiscoveryStartup` resolved `ICatalogIndexFreshness` and
   `AddDiscovery` registered nothing for it. It stayed invisible only because no deployment had configured the
   feature: the first one to try would have had the resolve throw, the surrounding catch hide the section, and
   the log say the vectors could not be checked — for ever.
6. **Two definitions of almost everything.** A renderer, a blob codec, an embedding port and an
   OpenAI-compatible adapter existed twice, once in each project, with independent tests.

The product owner chose to close all six.

## Decision

**D1 — The application adopts the tool's embedding contract.** A local OpenAI-compatible server, MiniLM,
width **384**. Not Foundry: ingestion and retrieval must embed through the same model, and the tool is the side
that has been run and verified.

**D2 — The port and the adapter are shared, not duplicated.** `IEmbeddingClient` and `EmbeddingRecipe` live in
`BuildingBlocks.Application`; `OpenAiCompatibleEmbeddingClient` lives in `BuildingBlocks.Infrastructure`. Both
composition roots resolve one implementation, so a query vector and a stored vector cannot be produced by two
implementations of one protocol that have quietly diverged.

**D3 — The application reads the tool's own file, and only reads it.** A new
`IProductVectorReader`, implemented over raw `Microsoft.Data.Sqlite`, opens the file the tool owns, reads the
vector table through the shared contract, and never writes. A file that is absent is refused rather than
created: opening a SQLite file creates it, which would leave an empty database behind that looked built.

**D4 — A product's passages are reduced to the one nearest the request.** `NearestChunk` is a pure rule in
`Application`, and `Similarity` is its own type because two callers now measure the same thing. Nearest rather
than average, because an average would make a score depend on how many passages a product happens to have.

**D5 — The tool writes a recipe table beside the vectors.** `product_embedding_recipe`, holding the model, the
width, the composition and the catalogue hash, written in the same transaction as the vectors. **This reverses
[0001](0001-catalog-ingestion.md)'s D4.** The gate in 1 is what the Host needs to hide a feature instead of
ranking vectors nobody can explain, and a recipe can only be trusted beside the rows it describes.

**D6 — The application's own ingestion is deleted.** `CatalogIngestion`, `ICatalogIngestion`,
`CatalogIngestionRequest`, `EmbeddedText`, `FoundryEmbeddingClient`, `CatalogIndex`, `CatalogVector` and their
configurations. The migration `VectorsMoveToTheIngestionTool` drops `Discovery_CatalogVector` and
`Discovery_Index`.

**D7 — The on-disk format and the file's names are one contract.** `VectorBlob` and `CatalogHash` move to the
shared kernel, joining `ProductVectorContract`, which names the two tables and the composition id. Two
implementations of one byte order is a disagreement that stays invisible until the writer and the reader meet.

**D8 — The freshness check is synchronous and takes the composition.** It reads a file with
`Microsoft.Data.Sqlite`, which is synchronous, and the application no longer renders a product's text — so the
composition is passed in from the shared contract rather than compared against a renderer this module no longer
has. It is also now **registered**, in `DiscoveryRegistration`, which is where the rest of the feature is.

## Consequences

**Good**

- There is one embedding implementation, one blob codec, one catalogue hash and one renderer.
- The vectors the tool wrote are finally read, so the work in 0001 and 0002 is load-bearing rather than inert.
- A request embedded at 384 is compared with vectors written at 384, which is the only arrangement in which the
  shortlist's scores mean anything.
- The unregistered-freshness-check defect is gone, and a configured deployment would now check rather than
  silently hide.
- The module shrank: the change is **+628 / −1424 lines** across the solution.

**Bad**

- **The application now needs a local embedding server in every environment.** There is no cloud deployment it
  can be pointed at instead; that was the point, and it is a deployment obligation rather than a code one.
- **The tool must be run before the application can search**, and what it wrote has to be reachable at the path
  the deployment configures. Nothing else produces vectors now.
- **The composition id lives away from the renderer.** Changing the fields a product's text is made of now means
  bumping `ProductVectorContract.Composition` in another project; the renderer's test is what makes that
  visible, and the freshness check is what makes forgetting it loud.
- **The recipe is a second table**, which 0001 deliberately avoided. The cost recorded there — no staleness
  check — is what it buys back.
- **Foundry is no longer reachable for embeddings.** The agent still uses it; embedding does not.

**Found while doing this, and not fixed**

- **The Host's Development test factories do not isolate their database.** They set `Sqlite:DatabasePath`
  through `ConfigureAppConfiguration`, which is applied at `Build()` — after `Program` has already read
  `builder.Configuration` and opened the file. So the tests migrate and read the developer's local
  `App_Data/corerental.db`. It was invisible while no module's migration changed the schema late in the list;
  this one exposed it, and a stale local database left from the reverted Discovery design fails 13 tests
  against a schema the code no longer has.

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Move the application to Foundry's width instead | The tool is the verified side and the vectors already exist; re-ingesting to match a cloud deployment reverses 0001 |
| Keep two embedding adapters, one per root | Two implementations of one protocol is the drift this pass exists to remove — and the vectors they produce are compared with each other |
| Give each project its own copy of the shared types | The same drift, one layer down: the point of the contract is that the writer and the reader cannot disagree |
| Have the application write the vectors instead | Ingestion is an operator's deliberate act with its own file; the application reading it is what 0001 established |
| Keep the application's ingestion as a second path | It was unreachable except from tests, and two ingestions of one catalogue is two answers to one question |
| Let the application render the product text again for the composition | A second renderer is a second definition of a product's text, and the two would diverge the first time a field was added |
| Compare only the model and width, without the composition | A renderer change would then be undetectable and the vectors silently incomparable — the exact failure the check exists for |
| Reduce a product's passages by averaging them | An average rewards a product for having few passages, which is a preference for short descriptions dressed up as relevance |
| Keep the freshness check asynchronous | It reads a file synchronously; a task around a blocking call claims an asynchrony that is not there |
| Point the tool at the application's database instead | It would make the tool's file-and-schema ownership in 0001 false, and hand a single-writer derivation into a database with other writers |

## Notes

- **Traceability.** The six divergences and where each landed:

  | Found | Landed as |
  |---|---|
  | 1 — incomparable vectors | **D1**, **D2** |
  | 2 — nothing read the tool's file | **D3** |
  | 3 — one vector per product | **D4** |
  | 4 — the gate had nothing to read | **D5**, **D6** |
  | 5 — the gate was unregistered | **D8**, and `DiscoveryRegistrationTests` is what would catch a repeat |
  | 6 — everything defined twice | **D2**, **D6**, **D7** |

- **The application is told where the tool's file is** through `VectorEmbedding:EmbeddingDatabase`, and the rest of
  its settings are the same three the tool takes: `VectorEmbedding:Server`, `VectorEmbedding:Model`,
  `VectorEmbedding:Width`.
  `appsettings.Development.json` points at the local server and at the tool's conventional output path, so a
  fresh clone can ingest and then run.
- **`System.ClientModel` gained a central pin**, at 1.15.0 rather than 1.14.0, because the tool names
  `ClientResultException` directly and the agent's Foundry client requires at least that version. One central
  version has to satisfy both.
- **The suite** is 815 passing across the ten offline projects with the local development database regenerated.
  The 13 failures on a machine that still holds a pre-`VectorsMoveToTheIngestionTool` database are the
  test-isolation defect above and not a property of this change.
