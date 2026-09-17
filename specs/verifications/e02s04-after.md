# e02s04 — the catalogue becomes three candidates

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `agentfoundry/AgentFoundry.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| TESTS `agentfoundry/AgentFoundry.sln` | 17 passed | **25 passed, 0 failed** |
| BUILD `CoreRentalNet.sln` | unmoved | **0 warnings, 0 errors — unmoved** |
| NONBROWSER `CoreRentalNet.sln` | 570 passed | **570 passed, 0 failed — unmoved** |

## What changed

- **`Catalogue/`** — the reader port and the shapes it answers with (`CatalogueItem`,
  `CatalogueMetadata`, `CataloguePage` including `Total`/`Truncated`), and
  `IncompleteCatalogueException`.
- **`Selection/SlotCatalogue`** — which products belong to which slot, with the asymmetry that is a
  measured property of the catalogue rather than a choice.
- **`Selection/TierPicker`** — price position, ties by SKU, and the pinning threshold.
- **`Selection/CriteriaMapper`** — tokens matched against the products actually chosen, and the
  customer's own words reported when nothing matches.
- **`Selection/Suggestor`** — one read, one candidate per tier, the all-pinned single candidate, and
  the truncation refusal.
- **`Workflow/SuggestorExecutor`** and the graph's third node.
- **`Specification` carries the query**, because the criteria are matched from the same words the slots
  came from and nothing downstream carried the request.

## Measured effect

| Request | Result |
|---|---|
| Desk + Monitor, 5 monitors priced 300…400 | monitors `MON-A / MON-C / MON-E` — cheapest, median index, most expensive |
| Desk + Lamp, one lamp in the catalogue | the lamp is the same SKU in all three options and `pinnedSlots` says so |
| Lamp only, one candidate | **one** candidate, not three identical ones |
| `"a standing desk with two monitors and something reliable"` | `slot:Desk`, `slot:Monitor`, `quantity:Monitor:2`, `tag:standing` matched; `"reliable"` reported in `unevaluated` |
| A truncated page | refused, not tiered |

## What the run corrected

1. **A count word was reported as a criterion the catalogue could not check.** "two monitors" produced
   `quantity:Monitor:2` *and* an `unevaluated` entry for "two", which would have told a customer that
   the catalogue could not express the number two. Count words are now consumed by the quantity rule
   and not reported.
2. **A failing node looked like a short run.** Microsoft Agent Framework ends the stream when an
   executor throws, so an unread `WorkflowErrorEvent` makes a failure indistinguishable from a request
   that produced no result — and the run would have reported success. The run now reads it and
   rethrows. This was found by a test whose fixture was one slot short: the failure it caused was
   invisible until the event was handled.
3. **The `Specification` had to carry the query.** The first draft threaded the request through the
   node, which produced a node holding a `Query` property and an empty slot list — a workaround for
   information the run genuinely needs. The words travel on the specification, which is where the
   criteria come from.
4. **`AGT-12` was re-worded.** It said "one catalogue read per request, reused across attempts"; with
   one attempt per run, the reuse half is not yet testable and the claim is now the one that is.

## Not done, deliberately

- **The HTTP client and its credential.** `ICatalogueReader` is a port; the adapter arrives with the
  deployment, because the auth path is what `e02s01` decides.
- **The across-attempts reuse.** One read per run is asserted; the loop that would make reuse matter
  arrives with the reviewer.
- **No criteria extraction from the request beyond words.** The mapper matches the request's words
  against what was chosen; a model that *parses* criteria out of a sentence arrives with the deployment,
  and the mapper's contract would not change if it did.
