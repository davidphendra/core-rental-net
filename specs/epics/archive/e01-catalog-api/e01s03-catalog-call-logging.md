# e01s03 — Every catalog call is attributable

**type:** feat
**risk:** P2
**context:** infra
**bcps:** 2
**status:** failing

## Context

The caller is an autonomous agent, so its calls are otherwise invisible. This story adds one structured
log line per request — the caller's identity, the filters it asked for, and the number of results —
written after authorization, so a refused call is distinguishable from an allowed read.

## Requirements

#### ADDED: One structured log line per catalog request

Each request to `GET /api/catalog` writes one `Information` log entry carrying the caller (the token's
`sub`, or the client id for a machine token), the three filter values as supplied (null when absent),
and the number of products returned. A refused request is not logged as a successful read; its outcome
comes from the authorization pipeline.

## Steps

1. Add the log call in the endpoint after authorization, with the structured properties `{Caller}`, `{Category}`, `{SubCategory}`, `{Search}`, `{Count}`. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Unit test the log fields through a capturing logger: each property is present and the count matches (CatalogApiLogTests). → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogApiLogTests"`
3. In-process HTTP test API-12: one request emits exactly one log entry with the expected properties. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogApiEndpointTests"`
4. Re-run the three baselines. → verify: `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E" --nologo`

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| API-12 | A request logs caller, filters and result count once | http |

## Out of scope

- Persisted audit records.
- Logging the response body.

## Risks

- **Noise.** One line per read is right in production and chatty in development; keep it `Information`,
  keep the body out of it, and do not log the same request twice.

## Acceptance criteria

- API-12 passes; no existing test counts change.
