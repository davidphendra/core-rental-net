# Catalogue endpoint discoverability — verification

Change: **A** — a request file (`CatalogApi.http`) — and **B** — a development-only OpenAPI document.
Follows the archived epic `e01`; branched from `main` @ `8c20769`.

## Baselines

| Baseline | Before | After | Result |
|---|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors | unchanged |
| NONBROWSER | 525 passed, 0 failed | 529 passed, 0 failed | +4 (3 document, 1 not-served guard) |
| BROWSER | 103 passed, 1 skipped | 103 passed, 1 skipped | unchanged |

## Test matrix

| ID | Scenario | Where it is proven |
|---|---|---|
| API-13 | The document is served in development, as JSON | `CatalogOpenApiTests.The_document_is_served_in_development` |
| API-13 | It describes the route, the three filters and the 200/400 answers | `CatalogOpenApiTests.The_document_describes_the_route_its_filters_and_its_answers` |
| API-13 | It writes the filter vocabulary as the words themselves | `CatalogOpenApiTests.The_document_writes_the_filter_vocabulary_as_strings` |
| API-13 | It is **not** served outside development | `CatalogApiEndpointTests.The_openapi_document_is_not_served_outside_development` |

## Real-host evidence

Development: `/openapi/v1.json` → **200**, 4 607 bytes; `paths` includes `/api/catalog`; parameters
`category`, `subCategory`, `search`; schemas `ProductView`, `CatalogCategory`, `CatalogSubCategory`,
`Money`; `CatalogCategory.enum` is `["chair", "desk", "accessory"]`.

Production: `/api/catalog` → **200** (open, no provider) and `/openapi/v1.json` → **404**.

## What changed

- `Microsoft.AspNetCore.OpenApi` 10.0.12, pinned.
- `CatalogApiRegistration` registers the document alongside the endpoint's JSON.
- `ApplicationPipeline` maps it in development only, through its own method.
- `CatalogController` carries the endpoint's metadata: name, summary, description, tag, and the
  `200`/`400`/`401`/`403` answers.
- `CatalogApi.http` — the same requests for a REST client, with the environment note.

## Notes

- **The coding-standard gate caught the first attempt.** Adding the mapping inline took
  `UseApplicationPipeline` to 44 lines, over the 40-line budget, and
  `CodingStandardTests.No_method_exceeds_the_length_or_nesting_budget` failed. It is now
  `MapDevelopmentDocumentation`. The guard did exactly what it exists for.
- The document is anonymous. That is deliberate: it describes the API, it is not the API, and it is
  served only where a caller's author needs it.
- `CatalogApi.http` has no test. It is a request file, not code; its value is that a developer can
  send the calls, and the calls it makes are the ones the API-01…API-12 tests already pin.
- The document names `/account/login` and `/account/logout`, which the account endpoints contribute.
  Nothing to describe them was added; they appear because they are mapped.
