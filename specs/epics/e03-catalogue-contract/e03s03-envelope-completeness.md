# e03s03 — The envelope says when an answer is incomplete

**type:** feat
**risk:** P1
**context:** api
**bcps:** 3
**status:** failing

## Context

`ApiCollection` reports `count`, and `count` is **the number returned**, not the number matched. So a
truncated answer is **indistinguishable from a complete one** — which matters more here than it looks,
because the tier rule takes the **median position** of a slot's candidates. Measured on the monitors:
8 candidates → `300 / 400 / 475`; 5 candidates → `300 / 350 / 400`. A caller that cannot tell "there
are 8 monitors" from "here are 5 of 8 monitors" computes a different middle tier and never knows.

This story makes an incomplete answer **say so**, and adds the defensive cap that makes the signal
mean something. It is the field the agent epic's tiering rule depends on.

## Requirements

#### ADDED: `total` and `truncated` on the envelope

`ApiCollection` reports **`total`** — how many products matched the filters — beside `count`, which
stays the number returned; and **`truncated`**, true exactly when `count < total`. A caller can then
distinguish a complete answer from a partial one without counting.

#### ADDED: a defensive `limit`

The endpoint accepts `limit`, defaulted to a documented maximum **above the catalogue's size**, so a
default request is complete and `truncated` is false. A `limit` below the number matched returns that
many rows with `truncated: true` and `total` unchanged — so growth in the catalogue cannot silently
turn into a partial answer.

## Zoom-Out

- **Module purpose:** the Host's catalogue envelope; it describes what was returned and what was
  matched, and those are no longer assumed to be the same number.
- **Callers:** the agent, which must refuse to tier a slot whose candidate set is truncated.
- **Contracts to preserve:** `value` and `count` keep their meanings, so no existing caller changes
  behaviour. No pagination cursor is introduced: the collection is a bounded in-memory snapshot and
  `nextLink` would be speculative (the scope records that omission deliberately).

## Steps

1. Add `total` and `truncated` to `ApiCollection` and to the responses that build it. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Add the documented default maximum as one constant, and bind `limit`. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
3. Apply the cap after filtering, computing `total` from the filtered set **before** the cap. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
4. Declare the new fields and the parameter in the OpenAPI document. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogOpenApiTests"`
5. HTTP tests API-36 … API-38, including the non-vacuity guard. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogApiEnvelopeTests"`
6. Re-run the three baselines. → verify: `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E" --nologo`

## Verification Script (Step-by-Step)

1. Start the host with no identity provider.
2. `curl -s "http://localhost:5199/api/catalog?view=compact" | jq '{count,total,truncated}'` →
   `count == total`, `truncated == false`.
3. `curl -s "http://localhost:5199/api/catalog?view=compact&limit=5" | jq '{count,total,truncated}'` →
   `count: 5`, `total: 140`, `truncated: true`.
4. `curl -s "http://localhost:5199/api/catalog?view=compact&subCategory=monitor&limit=5" | jq '{count,total,truncated}'`
   → `total: 20`, `truncated: true` — the total reflects the filter, not the catalogue.
5. Confirm a caller that ignores the new fields sees exactly what it saw before.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| API-36 | A complete answer reports `total == count` and `truncated: false` | http |
| API-37 | A capped answer reports `count < total` and `truncated: true` | http |
| API-38 | Non-vacuity: a response built without the truncation check fails API-37 | http |

## Out of scope

- `nextLink`, cursor pagination, or any ordering guarantee beyond catalog order.
- Any change to the filters, to `full`, or to the store's search.
- A cache or a rate limit on the endpoint.

## Risks

- **The cap creates the truncation it exists to report.** That is intended, but it moves a rule into
  the agent: **a slot whose candidate set is truncated must not be tiered**, since its median depends
  on how many rows arrived. That rule belongs to `e02s04` and is recorded there.
- **A default below the catalogue's size would silently change answers.** The default must be
  documented and asserted above the catalogue size (API-36), so growth is the only thing that can
  ever set `truncated` on an unqualified request.
- **`total` computed after the cap.** The most likely implementation error, and API-37 is what catches
  it — which is why the non-vacuity guard exists.

## Acceptance criteria

- API-36 … API-38 pass, with API-38 failing when the truncation check is removed.
- A request without `limit` is complete and unchanged for an existing caller.
- The document declares `limit`, `total` and `truncated`.
- BUILD and NONBROWSER are green.
