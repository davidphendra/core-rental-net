# e01s03 — catalog call logging — verification

Story: **e01s03 — Every catalog call is attributable** (2 BCPs, P2).
Branch: `catalog-api`, worktree `/Users/mac/Documents/AI Workspace/catalog-api`.

## Baselines

| Baseline | Before | After | Result |
|---|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors | unchanged |
| NONBROWSER | 519 passed, 0 failed | 523 passed, 0 failed | +4 (2 unit, 2 HTTP) |
| BROWSER | 103 passed, 1 skipped | 103 passed, 1 skipped | unchanged |

## Test matrix coverage

| ID | Scenario | Where it is proven |
|---|---|---|
| API-12 | One request logs the caller, the filters and the count | `CatalogApiLogTests` (the fields), `CatalogApiLoggingTests` (a real request) |
| API-08 | A refused request writes no catalogue line | `CatalogApiLoggingTests.A_refused_request_writes_no_catalogue_line` |

## What changed

- `Host/Controllers/CatalogApiLog.cs` — the line, with a named logger category constant so a test can
  find it. Filled only after the request is authorised and answered.
- `Host/Controllers/CatalogController.cs` — writes the line with the caller and the result count; the
  caller is the token's `sub` (or the mapped name identifier), and `anonymous` when the endpoint is
  open.
- Tests — `CapturingLoggerProvider` (records category, level, message and structured properties),
  `CatalogApiLogTests`, `CatalogApiLoggingTests`; `CatalogApiAuthorizedFactory` exposes the capture;
  `CatalogApiTestHandler` carries a subject so the caller can be asserted.

## Notes

- The line sits after authorisation, which is what makes a refused call distinguishable — and is
  asserted rather than assumed.
- An absent filter is recorded as null, not as an empty word.
- The response body is deliberately not logged: the count and the filters are the useful fields, and
  the body is the catalogue.
