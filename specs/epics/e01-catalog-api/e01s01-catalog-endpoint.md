# e01s01 — An agent lists and filters the catalog over HTTP

**type:** feat
**risk:** P0
**context:** infra
**bcps:** 5
**status:** failing

## Context

The application already has the one read operation an agent needs — `ISearchCatalogHandler` over
`SearchCatalogQuery` — and no way for a machine to reach it: the only HTTP surface is the sign-in and
sign-out redirects. This story adds `GET /api/catalog`, binds `category`, `subCategory` and `search`,
and serializes the module's published `ProductView`. It is the tracer bullet: route, binding, JSON
shape and the refusal of a bad filter, proven end to end in-process, with no authentication yet (the
endpoint is open where no provider is configured, which is the rule the catalog already follows).

## Requirements

#### ADDED: GET /api/catalog lists and filters the catalogue

`GET /api/catalog?category=&subCategory=&search=` returns `200` with a bare JSON array of the module's
`ProductView`. All three parameters are optional; omitting all returns the whole catalogue in catalog
order. `category` and `subCategory` accept the enum member names case-insensitively; an unknown value
returns `400` naming the allowed values. `search` narrows by product name only, as it already does for
the store. No match returns `200` with `[]`.

Enums are written as lowercase strings (`"chair"`, `"monitor"`), not numbers, so the value a caller
reads is the value it may send back. `Money` is written as `{ "amount": …, "currency": "IDR" }`.

## Zoom-Out

- **Module purpose:** the Catalog module presents an immutable, in-memory snapshot of `products.json`
  to callers; it has no write side.
- **Callers:** the Builder, the Store page and the Store/Buyer projections in the Blazor UI, through
  `ISearchCatalogHandler` and `IProductCatalog`.
- **Contracts to preserve:** `ProductView`, `CatalogCategory`, `CatalogSubCategory`, and `Search`'s
  documented behaviour ("a term matches a product's name only"). This story changes none of them; the
  endpoint is an adapter over them.

## Steps

1. Add `Host/Controllers/CatalogApiParameters.cs`: parse the three raw query values into a
   `SearchCatalogQuery` or a refusal message naming the allowed values. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Add `Host/Controllers/CatalogController.cs` with `MapEndpoints`, an `/api` group and
   `MapGet("catalog", …)` injecting `ISearchCatalogHandler`; a valid parse returns `Results.Ok(products)`,
   an invalid one `Results.BadRequest(message)`. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
3. Configure the endpoint's JSON: camelCase property names (the minimal-API default) and
   `JsonStringEnumConverter` with a camelCase naming policy. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
4. Call `CatalogController.MapEndpoints(app)` from `ApplicationPipeline`, always. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
5. Unit tests for binding and refusal (API-01, API-02) in `CoreRentalNet.Host.Tests`. → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogApiParametersTests"`
6. Add `Microsoft.AspNetCore.Mvc.Testing` [OK] to `Directory.Packages.props` and `CoreRentalNet.Host.Tests`, plus the in-process fixture (temp `Sqlite:DatabasePath`). → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
7. In-process HTTP tests for the behaviour and the body (API-03 … API-07). → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogApiEndpointTests"`
8. Re-run the three baselines. → verify: `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E" --nologo`

## Verification Script (Step-by-Step)

1. Start the host: `dotnet run --project src/Host/CoreRentalNet.Host`.
2. `curl -s "http://localhost:5000/api/catalog?category=desk" | jq 'length'` → 10.
3. `curl -s "http://localhost:5000/api/catalog?category=desk" | jq '.[0].category'` → `"desk"`.
4. `curl -s -o /dev/null -w '%{http_code}' "http://localhost:5000/api/catalog?category=sofa"` → 400, body names `chair`, `desk`, `accessory`.
5. `curl -s "http://localhost:5000/api/catalog?search=teak" | jq 'length'` → only products whose name contains "teak".
6. `curl -s "http://localhost:5000/api/catalog?search=zzz"` → `[]`.
7. `curl -s "http://localhost:5000/api/catalog" | jq 'length'` → 62.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| API-01 | Unknown `category`/`subCategory` → 400 naming the allowed values | unit |
| API-02 | Enum parsing is case-insensitive (`Desk`, `DESK`) | unit |
| API-03 | No filters → the whole catalog, in catalog order | http |
| API-04 | `category=desk` → ten desks; `subCategory=monitor` → eight monitors | http |
| API-05 | `search` narrows by name only | http |
| API-06 | No match → 200 `[]` | http |
| API-07 | Body shape: camelCase, lowercase enum strings, `Money` object, null `subCategory` | http |

## Out of scope

- Authentication (e01s02) and logging (e01s03).
- Pagination, envelope, OpenAPI, MCP.
- Any change to the store's or the builder's search.

## Risks

- **In-process host startup runs migrations.** `ApplyMigrationsInDevelopmentAsync` runs when the
  environment is Development; the fixture must point `Sqlite:DatabasePath` at a temp file so the suite
  does not touch `App_Data/corerental.db`.
- **Enum naming policy.** `JsonStringEnumConverter` writes the member name; without the camelCase naming
  policy the values arrive as `"Chair"`, breaking the vocabulary the agent sends back.

## Acceptance criteria

- API-01 … API-07 pass.
- `dotnet build` 0 warnings, 0 errors; the non-browser total rises only by the new tests.
