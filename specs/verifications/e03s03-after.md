# e03s03 — the envelope says when an answer is incomplete

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 564 passed, 0 failed | **570 passed, 0 failed** |
| BROWSER | 104 passed, 0 failed, 1 skipped | **unchanged** — an envelope concern; the pages read the module in process |

## What changed

- `ApiCollection<T>` and `CompactCatalogCollection` carry **`total`** beside `count`, and **`truncated`
  is derived** from the two rather than passed in, so it cannot disagree with them.
- `ApiCollection` takes `total`; `CatalogApiLimits.Default` is the documented cap for a request that
  names none.
- `CatalogController` counts the matches before capping, logs what actually came back, and caps with
  one helper.
- `CatalogQueryParameterTransformer` describes `view` and `limit`; it replaces the
  projection-only transformer from `e03s02`.

## Measured effect

| Request | `count` | `total` | `truncated` |
|---|---|---|---|
| no limit | 140 | 140 | false |
| `limit=5` | 5 | 140 | true |
| `subCategory=monitor&limit=5` | 5 | **20** | true |
| `limit=140` (the boundary) | 140 | 140 | false |
| `limit=139` (one under) | 139 | 140 | true |
| `limit=0` | — | — | 400 `api.invalid_request` |

## What the run corrected

1. **`truncated` is derived, not supplied.** The plan implied a flag passed alongside the counts,
   which can disagree with them. Computed from `count < total`, it cannot.
2. **`limit=0` is refused rather than clamped.** Quietly turning a caller's zero into one row is the
   kind of answer that looks like it worked; `[Range(1, …)]` lets `[ApiController]` answer it as a
   validation refusal, in the same problem-details shape as everything else.
3. **The cap is asserted at its boundary.** `API-38` checks 140 and 139 rather than only "a limit was
   sent", so an implementation that reported `truncated: true` for any capped request fails.
4. **The transformer was renamed.** It described one parameter and now describes two; leaving it called
   "projection" would have been a name that lies about what it does.

## Not done, deliberately

- **No paging.** No cursor, no `nextLink`, no ordering guarantee beyond catalogue order. The scope
  records that omission: the collection is a bounded in-memory snapshot, and an envelope that can
  report truncation is what makes paging addable without a break when it is earned.
- **No change to the log line's meaning.** It still records what actually came back, which is the
  contract `e01s03` published.
- **No cache.** 140 rows in memory, one projection per request.
