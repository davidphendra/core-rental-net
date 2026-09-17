# e03s02 — A machine caller reads only the fields it needs

**type:** feat
**risk:** P0
**context:** api
**bcps:** 5
**status:** passing

## Context

The agent reads the catalogue through `GET /api/catalog` and puts what it reads into a prompt. The
full `ProductView` carries fields the agent cannot use: `imagePath` is **23 %** of the payload and
`imageAvailable` plus `isFeatured` another 11 %, and the agent names SKUs — the **application**
resolves images. Measured, the full body is **20,688 bytes ≈ 5,172 tokens**, of which 34 % is display
data.

This story adds a **view** to the existing resource — not a second resource and not a second
description of a product — so a machine caller asks for the fields it reads. `description` and
`metadata` stay: they are what a request is matched against.

## Requirements

#### ADDED: `view=compact` on `GET /api/catalog`

The endpoint accepts `view` with the values `full` (the default, today's body) and `compact`. Compact
returns, for each product, **SKU, name, category, subcategory, price and description, plus metadata**
— and omits `imagePath`, `imageAvailable` and `isFeatured`. The price is a **number**, and the
**currency is stated once on the envelope** rather than repeated per row.

An unknown `view` value is refused `400` naming the allowed values, in the same shape as an unknown
filter, so a caller is told what would have worked.

#### ADDED: the catalogue is single-currency by contract

The envelope states one currency, which is only truthful because the catalogue holds one. A row whose
currency differs from the first is refused **at load time**, naming the SKU — rather than being
discovered later as a wrong total.

## Zoom-Out

- **Module purpose:** the Host describes the Catalog module's published view over HTTP to a machine
  caller.
- **Callers:** the agent, through its catalogue client. The UI does **not** use HTTP — the store and
  the builder inject `ISearchCatalogHandler` in-process — so this story changes no page.
- **Contracts to preserve:** the envelope shape `{ value, count, … }`; the existing filters and their
  refusals; `full` remains the default, so today's callers see today's body. `ProductView` stays the
  only description of a product — compact is a **projection** of it, not a second type.

## Steps

1. Add the view vocabulary as one type naming the allowed values, used by the binder and by the
   refusal. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Bind `view` in `CatalogController` with a default of `full`, refusing an unknown value with the
   allowed list in problem details. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
3. Add the compact projection as one mapper from `ProductView` to the compact shape, and serialize
   the compact envelope with a single `currency`. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
4. Make the projection discoverable in the document. A status code has **one schema per content
type**, so declaring a second `200` replaces the first and the document would describe the compact
answer while claiming it is the default. The vocabulary therefore travels on the `view` parameter,
added by an operation transformer, and the document keeps describing `full` as the default. →
   verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogOpenApiTests"`
5. Add the load-time single-currency guard to `ProductLoader`, naming the offending SKU. → verify: `dotnet test tests/CoreRentalNet.IntegrationTests --nologo --filter "FullyQualifiedName~Currency"`
6. HTTP tests: the compact body and its omissions (API-32), the single currency (API-33), the
   refusal (API-34), and the non-vacuity guard (API-35). → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~CatalogApiCompactViewTests"`
7. Re-run the three baselines. → verify: `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E" --nologo`

## Verification Script (Step-by-Step)

1. Start the host with no identity provider, as the machine caller meets it.
2. `curl -s "http://localhost:5199/api/catalog?view=compact" | jq '.value[0]'` → 7 fields and a
   numeric price; **no** `imagePath`, `imageAvailable` or `isFeatured`.
3. `curl -s "http://localhost:5199/api/catalog?view=compact" | jq '.currency'` → `"IDR"` once.
4. `curl -s "http://localhost:5199/api/catalog?view=full" | jq '.value[0] | has("imagePath")'` → `true`.
5. `curl -s -o /dev/null -w '%{http_code}' "http://localhost:5199/api/catalog?view=wide"` → `400`,
   body naming `full` and `compact`.
6. `curl -s "http://localhost:5199/api/catalog?view=compact" | wc -c` → materially smaller than the
   same call without `view`.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| API-32 | Compact omits the image and display fields | http |
| API-33 | One currency on the envelope; a mixed-currency catalogue fails the load | http + unit |
| API-34 | An unknown view is refused naming the allowed values | http |
| API-35 | Non-vacuity: removing the projection fails API-32 | http |

## Out of scope

- `total` and `truncated` (e03s03).
- Any change to `full`, to the filters, or to the store's search.
- A typed client generated from the document.

## Risks

- **A projection that drifts from the record.** Compact must be derived from `ProductView`, not
  hand-listed, or the two will disagree as fields are added. One mapper, asserted by API-32.
- **The document cannot show two `200` shapes.** Measured on the first run: a second
  `[ProducesResponseType]` for the same status and content type replaced the first, and the document
  then described the compact envelope as the default. The vocabulary travels on the parameter
  instead, which is a real limitation for a generated client - it will not model the compact body -
  and it is recorded rather than papered over.
- **A second currency creeping in.** The envelope states one currency, which the loader makes
  truthful by refusing any row priced in anything else; without that rule the one figure would be a
  guess about the rows.

## Acceptance criteria

- API-32 … API-35 pass, with API-35 failing when the projection is removed.
- `full` is unchanged for an existing caller.
- The document describes both views.
- BUILD and NONBROWSER are green; the browser suite is untouched.
