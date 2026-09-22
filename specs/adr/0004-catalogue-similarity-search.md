# 0004 — The catalogue is searchable by meaning, behind its own permission

**Status:** accepted
**Date:** 2026-09-21
**Deciders:** product owner, engineer

## Context

[0001](0001-catalog-ingestion.md) built the vectors, [0003](0003-discovery-reads-the-tool.md) made the
application read them, and both left search as follow-on work: the vectors were only ever reached by the
suggester's shortlist. The catalogue's own endpoint still searched with
`IProductCatalog.Search` — a case-insensitive substring of a product's **name** — so a caller asking for
"a quiet corner with two screens" got nothing unless a product happened to be named that way.

The retrieval pieces already exist and are shared: `IEmbeddingClient` embeds the request at the same model
and width the tool wrote at, `IProductVectorReader` reads the tool's table, `NearestChunk` reduces a
product's passages to its nearest one, and `Similarity` measures two vectors. What was missing was a
**consumer** for them outside the suggester, and a decision about where it may live.

The obvious place — `SearchCatalogHandler`, in `Catalog.Application` — cannot reach them directly:
`Discovery.Application` already references `Catalog.Application` (the shortlist is expressed in the
catalogue's vocabulary, `CatalogBucket`), so `Catalog.Application` → `Discovery.Application` is a
**project-reference cycle**. The dependency can only run one way, or be inverted.

## Decision

**D1 — The port is declared by the module that needs it, and implemented by the module that owns the
vectors.** `IProductSimilarity` and `ProductSimilarity` live in
`Catalog.Application.Contracts`; `SqliteProductSimilarity` lives in `Discovery.Infrastructure`, which
already references `Catalog.Application` through `Discovery.Application`. That is dependency inversion,
not a reach across a module boundary: the catalogue states what it needs, the composition root wires the
adapter, and no cycle is created.

**D2 — The similarity search is its own endpoint and its own permission.** `GET /api/catalog/similarity`
behind the `SimilaritySearch` policy (`Authorization:SimilaritySearch`) — a deployment configures the claim,
and `searchsimilarity:aibuilder` is the value used here. The published `GET /api/catalog` and its
`read:catalog` permission are untouched.

**D3 — Authorization gates, routing selects.** No factory chooses a handler from the caller's claims. The
route names the capability, the policy says who may use it, and each action calls exactly one handler.
A caller without the permission is refused `403` by the pipeline before the action runs.

**D4 — The filters narrow, the similarity orders, the cap truncates.** `category` and `subCategory` decide
what is eligible; the answer is those products nearest the sentence, nearest first, tied by SKU. A product
with no stored vector cannot be ranked and is left out. `total` is what was eligible and carryable,
`count` is what the answer carried — the same two numbers the published view already keeps apart. The
sentence is required; a request without one is a `400`.

**D5 — An unusable index is a 503, never a silent fallback.** A missing or stale vector file, or an
embedding server that did not answer, all arrive as `ProductSimilarityUnavailableException` and are mapped
by one exception handler to `503` with `catalog.similarity_unavailable`. There is deliberately **no**
fallback to the name search: turning "we could not retrieve" into "here are some name matches" is the
failure nobody reports, and it is the rule [0003](0003-discovery-reads-the-tool.md) already set for the
suggester.

**D6 — The start-up freshness verdict refuses the search.** `CatalogIndexAvailability` records the verdict;
a stale index refuses the similarity search through a decorator, so the action has no failure branch. It is one
type because the search is the only feature the verdict now decides: the AI builder this record originally
named was removed with the shortlist it was built on, and the "both features" wording went with it.

## Consequences

**Good**

- The vectors the ingestion tool writes now have a second, directly callable consumer, reachable without a
  suggestion run.
- The catalogue's plain read is unchanged: existing callers, tests and contracts are unaffected, and a
  deployment that does not want to embed pays nothing.
- The permission is separate, so a deployment can allow reading the catalogue and not searching it.
- Staleness is refused rather than ranked on, so a wrong-but-plausible answer is not available.

**Bad**

- **A second endpoint and a second permission to configure.** A deployment that wants similarity search
  must set `Authorization:SimilaritySearch`; unconfigured, it is closed and the endpoint answers 403.
- **The catalogue's search now has two answers to "find me a desk".** The name search and the meaning
  search are different endpoints with different orders, and a caller has to choose.
- **Every query reads the whole vector table.** It is bounded (hundreds of rows) and in-process, but it is a
  per-request file read where the shortlist's was per-run. A cache is a later change if it is ever felt.
- **The two dependency directions now meet in one solution.** Catalog declares a port Discovery implements;
  a reader has to know the reference direction to see why. The architecture tests hold the line (ARC-01).

## Alternatives rejected

| Alternative | Why rejected |
|---|---|
| Search inside `SearchCatalogHandler` | `Discovery.Application` already references `Catalog.Application`; the reverse is a project-reference cycle |
| A factory that picks a handler from the caller's claims | It re-implements authorization outside the policy pipeline, hardcodes a configured claim in code, bypasses 401/403 and scheme selection, breaks DI lifetimes (service locator), and dispatches on *who* rather than *what was asked for* |
| One endpoint, a mode chosen by entitlement | The same URL would mean different things to different callers; OpenAPI cannot describe it, caching cannot reason about it, and it is harder to test than two routes |
| Move `IProductVectorReader`/`NearestChunk` into `BuildingBlocks` so Catalog can own it | Reverses [0003](0003-discovery-reads-the-tool.md) D3 and puts retrieval's rules in the shared kernel for one caller |
| A second, private vector reader in `Catalog.Infrastructure` | The duplication [0003](0003-discovery-reads-the-tool.md) D2/D6 exists to remove; the writer and the reader of one file must stay one implementation |
| Fall back to the name search when embeddings are unavailable | Turns a retrieval outage into a bad answer, silently - the failure this arrangement exists to make loud |
| Rank on a stale index and let the operator notice | Every stored vector is invalidated silently by a model, width, composition or catalogue change; the answer would look healthy and be wrong |
| Expose the cosine score on the wire | `ProductView` and the envelope are asserted field by field, and a score is not stable across models or meaningful on its own |

## Baselines

Recorded at the end of this change:

| Baseline | Measured |
|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln` — **0 warnings, 0 errors** |
| `NONBROWSER` | **840 passed, 0 failed** across the ten offline projects (was 815; +25 for this change) |
| `BROWSER` | not run; unchanged by this work |
| `AGENT` | not run; unchanged by this work |

`ARCHITECTURE` is one of the ten and passes: one type per file, the coding budgets, and ARC-01 (no module
reaches another's Domain or Infrastructure) all hold with the inverted port.

## Notes

- **The permission is configuration, not a code constant.** `SimilaritySearchPolicy` names the policy;
  `Authorization:SimilaritySearch:ClaimType`/`:ClaimValue` say what entitles a caller, exactly as
  `CatalogRead` already does. `searchsimilarity:aibuilder` is the value this repository's deployment sets,
  not a value the application knows.
- **Unconfigured means closed.** With no identity provider the endpoint answers `403`, because a capability
  that spends money is closed rather than open; the hermetic suite drives the happy path through a factory
  that configures the permission and stands in for the vector port.
- **The OpenAPI document names both permissions.** `Auth0SecuritySchemeTransformer` now declares a
  requirement per gated path, so a generated client asks for `read:catalog` for the published view and
  `searchsimilarity:aibuilder` for the similarity search.
