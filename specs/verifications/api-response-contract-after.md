# The API response contract — what was run and observed

Implements `specs/API_RESPONSE_CONTRACT_LATEST.md`. Branch `api-responses`, one commit.

## What changed

| File | Change |
|---|---|
| `Presentation/ApiCollection.cs` | **New.** The collection answer: `value` and `count`, named after the fields the Microsoft REST API Guidelines prescribe. |
| `Infrastructure/ApiErrorCode.cs` | **New.** The documented, machine-readable codes, and the rule that a request which knows what went wrong keeps its own code while everything else falls back to its status. |
| `Infrastructure/ApiProblemDetails.cs` | **New.** Fills in `status`, `instance`, `traceId` and `code` on every problem the application produces. |
| `Composition/ApiResponseRegistration.cs` | **New.** Registers the problem-details service. |
| `Controllers/CatalogController.cs` | Returns the envelope; documents every status it can answer, each with the media type it actually arrives as. |
| `Controllers/CatalogEnumBinder.cs` | Names the code when it refuses a filter. |
| `Controllers/CatalogRoutes.cs` | Gains `Prefix`, the boundary the response pipeline is scoped to. |
| `Composition/ApplicationPipeline.cs` | Mounts the exception handler and the status-code pages for `/api` only; **removes** `UseExceptionHandler("/error")`, which pointed at nothing. Restructured into named steps (the architecture gate caught the method at 56 lines against a 40-line budget). |
| `Program.cs` | `AddApiResponses()`. |

## The contract, measured

Live host, `127.0.0.1`, identity off:

| Case | Before | After (measured) |
|---|---|---|
| 200 | `[{…}]` | `{"value":[{…}],"count":62}` |
| 400 unknown filter | bare string | `problem+json`, `code: catalog.unknown_filter`, `errors.category[0]`, `instance`, `traceId` |
| 401 no token | empty body | `problem+json`, `code: catalog.unauthenticated`, `traceId` |
| 403 wrong permission | empty body | `problem+json`, `code: catalog.not_permitted` |
| 404 unknown route under `/api` | empty body | `problem+json`, `code: api.unknown_route`, `type` = RFC 9110 §15.5.5 |
| 405 wrong verb | empty body | `problem+json`, `code: api.method_not_allowed`, §15.5.6 |
| 500 unhandled exception | **404**, empty body | **500** `problem+json`, `code: api.unhandled`, `traceId`, and **no exception text** |
| 404 for a *page* | empty | unchanged — still not JSON |

## Two defects found while implementing, both fixed

**1. The broken error path is gone.** `UseExceptionHandler("/error")` pointed at a path nothing served —
measured: `GET /error` → `404`, `Content-Length: 0`. An unhandled production exception was therefore
reported as a 404 with an empty body: the wrong status and nothing to diagnose with. The API now has its
own handler; the browser's case is recorded as an open gap below rather than papered over.

**2. The document described content types the endpoint never sends.** Every response was documented as
`text/plain`, `application/json` **and** `text/json`, and the refusals as `application/json` — where the
wire sends `application/problem+json`. A generated client was told to expect three shapes and the wrong
one for errors. Each `[ProducesResponseType]` now names its media type, and `API-13` asserts the exact
set: `application/json` for 200, `application/problem+json` for every refusal, each carrying its schema.

## Non-vacuity — three mechanisms, each removed in turn

| Removed | Result |
|---|---|
| `api.UseStatusCodePages()` | **4 tests fail** — the 404, the 405, the 401 and the 403 all lose their bodies |
| `api.UseExceptionHandler()` | **1 test fails** — the failure path stops producing a 500 |
| `ApiErrorCode.Set(…)` in the binder | **1 test fails** — measured: *"Expected `code` … but they differ at index 0"*, because the code falls back from `catalog.unknown_filter` to `api.invalid_request` |

Each was restored and the whole suite re-run green afterwards.

## Baselines

| Baseline | Before | After |
|---|---|---|
| BUILD | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 542 | **548** (+6) |
| BROWSER | 104 passed, 1 skipped | **104 passed, 1 skipped** |

## Deliberate deviations from the plan

- **No `IOpenApiOperationTransformer`.** The plan named one, following the 20,556★ reference. With a
  single operation it would hide the contract from the route it belongs to; the statuses and media types
  are declared on the action instead, which also gives the generator the `ProblemDetails` schema. A
  transformer earns its keep at the second or third controller.
- **`instance` is set in the customisation rather than left to the framework.** The framework fills it
  for the validation refusal and not for the status-code pages, and one field that appears sometimes is
  worse than one that always does.
- **No paging.** 62 items, and the envelope is what makes it addable without a break — which is the
  guidelines' actual concern.

## Open, recorded rather than hidden

The **non-API** unhandled-exception path now yields a 500 with an empty body instead of the previous
empty 404: honest, but still unhelpful to a person. A page-shaped error page needs a Blazor component
and a status code it can set, and is its own piece of work. It is not fixed by mounting `UseStatusCodePages`
globally, which would answer page navigations with JSON.

## What a caller can now do that it could not before

Branch on one field (`code`) instead of parsing prose; quote one identifier (`traceId`) that appears on
every refusal and is the same identifier the server logged; read the same shape whichever way the
request was refused; and tell "there are none of those" from "there is no such endpoint".
