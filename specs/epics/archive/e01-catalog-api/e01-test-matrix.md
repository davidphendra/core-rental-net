# e01 — Catalog API test matrix

Risk drives `verify-work` depth. Levels: `unit` = `CoreRentalNet.Host.Tests` without a host;
`http` = in-process `WebApplicationFactory` request.

| ID | Story | Scenario | Level | Risk |
|----|-------|----------|-------|------|
| API-01 | e01s01 | Unknown `category` / `subCategory` → 400 naming the allowed values | unit | P1 |
| API-02 | e01s01 | Enum parsing is case-insensitive (`Desk`, `DESK`) | unit | P1 |
| API-03 | e01s01 | No filters → the whole catalog, in catalog order (62) | http | P1 |
| API-04 | e01s01 | `category=desk` → ten desks; `subCategory=monitor` → eight monitors | http | P1 |
| API-05 | e01s01 | `search` narrows by product name only | http | P1 |
| API-06 | e01s01 | No match → 200 `[]` | http | P2 |
| API-07 | e01s01 | Body shape: camelCase, lowercase enum strings, `Money` object, null `subCategory` | http | P0 |
| API-08 | e01s02 | Identity configured, no token → 401 | http | P0 |
| API-09 | e01s02 | Valid token without `read:catalog` → 403 | http | P0 |
| API-10 | e01s02 | Valid token with `read:catalog` → 200 | http | P0 |
| API-11 | e01s02 | No provider configured → 200 (open) | http | P0 |
| API-12 | e01s03 | One request logs caller, filters and result count once | http | P2 |

## Baselines

| Baseline | Before | Expected after |
|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors |
| NONBROWSER | 492 passed | 492 + the new tests, 0 failed |
| BROWSER | 103 passed, 1 skipped | unchanged |

## Non-goals in this matrix

- No browser scenario: the endpoint has no UI, and the store/builder search behaviour is unchanged
  (API-05 pins that the endpoint reuses the name-only search).
- No load or rate-limit scenario: the caller is a single server-side agent.
