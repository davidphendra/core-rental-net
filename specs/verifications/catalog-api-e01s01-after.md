# e01s01 — catalog endpoint — verification

Story: **e01s01 — An agent lists and filters the catalog over HTTP** (5 BCPs, P0).
Branch: `catalog-api`, worktree `/Users/mac/Documents/AI Workspace/catalog-api`.

The endpoint is a Host-only adapter. The Catalog module was not touched: the route calls
`ISearchCatalogHandler` with a `SearchCatalogQuery` and returns the module's published `ProductView`.

## Baselines

| Baseline | Before | After | Result |
|---|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors | unchanged |
| NONBROWSER | 492 passed, 0 failed | 512 passed, 0 failed | +20 (12 binder unit, 8 in-process HTTP) |
| BROWSER | 103 passed, 1 skipped | 103 passed, 1 skipped | unchanged |

## Test matrix coverage

| ID | Scenario | Where it is proven |
|---|---|---|
| API-01 | Unknown `category`/`subCategory` → 400 naming the allowed values | `CatalogApiParametersTests`, `CatalogApiEndpointTests.An_unknown_category_is_a_400_naming_the_allowed_values` |
| API-02 | Enum parsing is case-insensitive; a number is not a name | `CatalogApiParametersTests` theory, `CatalogApiEndpointTests.A_filter_is_matched_ignoring_case` |
| API-03 | No filters → the whole catalog, in catalog order | `CatalogApiEndpointTests.No_filter_returns_the_whole_catalogue_in_catalog_order` (62, first "Seminyak Lounge") |
| API-04 | `category=desk` → ten desks; `subCategory=monitor` → eight monitors | `CatalogApiEndpointTests` (both) |
| API-05 | `search` narrows by name only | `CatalogApiEndpointTests.A_search_narrows_by_name_only` |
| API-06 | No match → 200 `[]` | `CatalogApiEndpointTests.No_match_is_an_empty_list_not_a_404` |
| API-07 | Body shape: camelCase, lowercase enum strings, `Money` object, null `subCategory` | `CatalogApiEndpointTests.The_body_is_camel_case_with_lowercase_enum_strings_and_a_money_object` |

## What changed

- `Host/Controllers/CatalogApiBinding.cs` — a result: the query to answer, or the refusal to return.
- `Host/Controllers/CatalogApiParameters.cs` — binds the three filters; matches enum names ignoring
  case and space; refuses an unknown value or a bare number, naming the wire vocabulary.
- `Host/Controllers/CatalogController.cs` — `MapEndpoints` maps `GET /api/catalog` onto
  `ISearchCatalogHandler`; a bad filter is `400`, otherwise `200` with the list.
- `Host/Composition/CatalogApiRegistration.cs` — `JsonStringEnumConverter` with the camelCase naming
  policy, so enums are written as the words the caller may send back.
- `Host/Composition/ApplicationPipeline.cs` — maps the endpoint always, provider or not.
- `Host/Program.cs` — calls `AddCatalogApi()`.
- `tests/CoreRentalNet.Host.Tests/` — `CatalogApiFactory` (in-process host, identity off by
  configuration, throwaway SQLite path), `CatalogApiParametersTests`, `CatalogApiEndpointTests`.
- `Directory.Packages.props` — `Microsoft.AspNetCore.Mvc.Testing` 10.0.12, pinned.

## Notes

- No authentication yet: that is e01s02. With identity configured the endpoint is currently open,
  which is why e01s02 must land before any deployment.
- The two-commit RED/GREEN policy could not be enforced mechanically: this repo has no `scripts/`
  (`verify-tdd-red-commit.sh` is absent). RED and GREEN were each demonstrated by running the tests.
