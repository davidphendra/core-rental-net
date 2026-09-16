# Traceability — epic e01 (catalogue API)

Refute framing: this was written by trying to disprove completeness — looking for a story with no
code, code with no test, a matrix row with no test, or a story with no evidence — and failing.

## Story → code → tests → evidence

| Story | Code added | Tests | Evidence |
|---|---|---|---|
| e01s01 | `CatalogApiParameters`, `CatalogApiBinding`, `CatalogController`, `CatalogApiRegistration` | `CatalogApiParametersTests` (12), `CatalogApiEndpointTests` (8), `CatalogApiFactory` | `e01s01-verify.yaml`, `catalog-api-e01s01-after.md` |
| e01s02 | `CatalogApiPolicy`, `CatalogApiAuthentication`, `AuthorizationRegistration.AddCatalogApiAuthorization`, `CatalogController.RequireAuthorization` | `CatalogApiPolicyTests` (5), `CatalogApiAuthorizationTests` (4), `CatalogApiAuthorizedFactory`, `CatalogApiTestHandler` | `e01s02-verify.yaml`, `catalog-api-e01s02-after.md`, `NFR-e01.json` |
| e01s03 | `CatalogApiLog`, the call in `CatalogController` | `CatalogApiLogTests` (2), `CatalogApiLoggingTests` (2), `CapturingLoggerProvider` | `e01s03-verify.yaml`, `catalog-api-e01s03-after.md` |

## Matrix row → test

| Row | Story | Test | Status |
|---|---|---|---|
| API-01 | e01s01 | `CatalogApiParametersTests` (unknown category/subcategory) + `CatalogApiEndpointTests.An_unknown_category_is_a_400_naming_the_allowed_values` | passing |
| API-02 | e01s01 | `CatalogApiParametersTests` (case, padding, numbers) + `CatalogApiEndpointTests.A_filter_is_matched_ignoring_case` | passing |
| API-03 | e01s01 | `CatalogApiEndpointTests.No_filter_returns_the_whole_catalogue_in_catalog_order` | passing |
| API-04 | e01s01 | `CatalogApiEndpointTests` (category, subcategory) | passing |
| API-05 | e01s01 | `CatalogApiEndpointTests.A_search_narrows_by_name_only` | passing |
| API-06 | e01s01 | `CatalogApiEndpointTests.No_match_is_an_empty_list_not_a_404` | passing |
| API-07 | e01s01 | `CatalogApiEndpointTests.The_body_is_camel_case_with_lowercase_enum_strings_and_a_money_object` | passing |
| API-08 | e01s02 | `CatalogApiAuthorizationTests.Without_a_token_it_refuses_with_401` + `CatalogApiLoggingTests.A_refused_request_writes_no_catalogue_line` | passing |
| API-09 | e01s02 | `CatalogApiAuthorizationTests` (no permission; contains-but-is-not) | passing |
| API-10 | e01s02 | `CatalogApiAuthorizationTests.With_the_configured_permission_it_answers_with_the_catalogue` + `CatalogApiPolicyTests` (scheme registered and bound to the API) | passing |
| API-11 | e01s02 | `CatalogApiEndpointTests` (all, identity unconfigured) + `CatalogApiPolicyTests` (no scheme named; audience required where one is registered) | passing |
| API-12 | e01s03 | `CatalogApiLogTests` (fields) + `CatalogApiLoggingTests` (a real request) | passing |

## Scope coverage

Every `in_scope` item in `specs/product/SCOPE_LATEST.yaml` maps to a story (e01s01, e01s02, e01s03),
and every story is in scope. Every `out_of_scope` item is absent from the diff: no MCP/OpenAPI wiring,
no OpenAPI document, no pagination or envelope, no broadened search, no alternative credential, no
module change, no write surface.

## Verdict: PASS

No dark story, no untested behaviour, no unverified story, no stale security review (addendum appended
after the review response), and all three baselines green at the released commit `9122b65`.
