# e03s01 — The catalogue carries what a request can be matched against

**type:** feat
**risk:** P0
**context:** module
**bcps:** 8
**status:** failing

## Context

Measured on the catalogue as it stands: **62 products**, **12 distinct description phrases**, and
after the product name is removed **exactly one phrase per slot** for desk, lamp and monitor. The
word `"monitor"` appears in **0 of 8** monitors and `"desk"` in **0 of 10** desks, because the
category is a **field, not text**. So a request like *"a reliable monitor for office setup"* matches
nothing: the only text that differs between two monitors is a Balinese place name plus a spec.

This story makes the catalogue worth matching against, in the one way that stays deterministic:
**more products, and a typed metadata record on each**. It is the ground the agent epic stands on —
without it, `C3` reports almost every criterion as `unevaluated`.

## Requirements

#### MODIFIED: the catalogue file

**Before:** 62 products — 10 desks, 10 chairs, 8 monitors, 8 plants, 6 lamps, 10 coffee, 10 beanbags.
Descriptions are `<name>: <oneFixedPhrasePerSlot>`, so two monitors read identically.

**After:** **140 products — 20 in each slot** (20 desk, 20 chair, 20 each of beanbag, coffee, lamp,
monitor, plant). Descriptions differ **within** a slot, not only across slots. Prices stay inside
today's bands — accessories 100,000–800,000, chairs 400,000–750,000, desks 600,000–1,500,000 — on
**round increments** (25,000 for accessories, 50,000 for desks and chairs), currency IDR. Exactly
**two** products are featured. Every product carries a local `/placeholders/*.svg` image path.

#### ADDED: a typed metadata record

Every product carries:

```json
"metadata": {
  "tags":       ["ergonomic", "mesh-back", "deep-work", "adjustable"],
  "attributes": { "type": "task", "back": "mesh", "headrest": "true" },
  "bestFor":    ["coding", "long sessions"],
  "notFor":     ["gaming"]
}
```

Typed, not free-form, because a free-form object cannot be validated and cannot yield a predictable
criteria vocabulary. `attributes` values are strings; `tags` are trimmed and de-duplicated; a blank
tag, an unknown key, or a non-string attribute value fails the load naming the SKU and the field,
exactly as the other fields already do.

#### MODIFIED: `ProductView` publishes the metadata

**Before:** `Sku, Name, Category, SubCategory, MonthlyPrice, Description, ImagePath, ImageAvailable,
IsFeatured`.

**After:** the same fields plus **`Metadata`** — one description of a product, for every consumer.

#### ADDED: the criteria vocabulary

Metadata yields the tokens `C3` maps a request onto: **`tag:<token>`** and
**`attribute:<SlotId>:<key>:<value>`**. A token either resolves to products or it does not, so a
criterion's disposition is an exact expectation rather than a judgement.

## Zoom-Out

- **Module purpose:** the Catalog module presents an immutable, in-memory snapshot of
  `src/shared/data/products.json` to callers; it has no write side.
- **Callers:** the Builder, the Store page and the Store/Buyer projections through
  `ISearchCatalogHandler` and `IProductCatalog`; and, from the agent epic, the HTTP endpoint.
- **Contracts to preserve:** `Search`'s documented behaviour (*a term matches a product's name
  only*) is **unchanged** — this story adds a field, it does not widen the search. `EnsureShape`
  (desks and chairs carry no subcategory) still holds. The currency stays IDR.

## Steps

1. Generate the 140 products with a scratch script outside the repository, seeded so the same seed
   produces the same file, and commit only `src/shared/data/products.json`. → verify: `python3 -c "import json;d=json.load(open('src/shared/data/products.json'));print(len(d))"` → `140`
2. Add `ProductMetadata` to the Catalog domain as a record with `Tags`, `Attributes`, `BestFor`,
   `NotFor`, and add it to `Product`. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
3. Extend `ProductJsonRecord` with `metadata` and teach `ProductLoader` to map and **validate** it,
   naming the SKU and field on failure. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
4. Add `Metadata` to `ProductView` and map it in `ProductViewMapper`. → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
5. Add the test that the matching vocabulary is well formed - tags and attribute keys lower case,
   hyphenated, unique and non-blank - so `tag:4k` either matches or the catalogue is refused. No
   resolver type is added: nothing in this epic resolves a token, and a type no caller uses would be
   an abstraction bought for nothing. → verify: `dotnet test tests/CoreRentalNet.IntegrationTests --nologo --filter "FullyQualifiedName~The_matching_vocabulary_is_closed_and_predictable"`
6. Rewrite `CatalogFileTests` for the new counts and the metadata invariants (API-24 … API-28). →
   verify: `dotnet test tests/CoreRentalNet.IntegrationTests --nologo --filter "FullyQualifiedName~CatalogFileTests"`
7. Keep the five vendored images and give five products a remote image path whose SKU matches the
   file, so the vendoring rule and the photographed-product rendering are still exercised. Restore
   the real-catalogue assertion (API-29) and add a synthetic case beside it. Remove the vendored
   images only if that coverage is deliberately dropped. →
   verify: `dotnet test tests/CoreRentalNet.IntegrationTests --nologo --filter "FullyQualifiedName~VendoredImageTests"`
8. Unit tests for metadata validation and the criteria vocabulary (API-27, API-31). → verify: `dotnet test tests/CoreRentalNet.IntegrationTests tests/CoreRentalNet.Modules.Catalog.UnitTests --nologo --filter "FullyQualifiedName~Metadata"`
9. Update the body-shape assertion so every product carries metadata (API-30), and re-run the API
   endpoint and browser suites for the new counts. → verify: `dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E" --nologo`
10. Re-run the three baselines and record them in `specs/verifications/e03s01-after.md`. → verify: `dotnet test tests/CoreRentalNet.E2E --nologo`

## Verification Script (Step-by-Step)

1. `python3 -c` over the file: 140 rows, 20 per slot, every row carrying metadata, exactly 2 badges.
2. Start the host and `curl -s http://localhost:5199/api/catalog | jq '.count'` → `140`.
3. `curl -s http://localhost:5199/api/catalog | jq '[.value[].metadata] | length'` → `140`.
4. `curl -s http://localhost:5199/api/catalog?subCategory=monitor | jq '.value[0].metadata'` → a
   populated object, and **not** the same object as another monitor's.
5. Confirm the store page still searches by name only, and that two monitors now read differently.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| API-24 | 140 products, 20 per slot | integration |
| API-25 | Every product carries a valid metadata record | integration |
| API-26 | Two products in a slot differ in metadata | integration |
| API-27 | Malformed metadata fails the load naming SKU and field | unit |
| API-28 | Exactly two featured; unknown vocabulary rejected | integration |
| API-29 | The vendoring rule is exercised independently of the data file | unit |
| API-30 | The published body carries metadata | http |
| API-31 | The criteria vocabulary is closed | unit |

## Out of scope

- The compact projection (e03s02) and the envelope (e03s03).
- Any change to `Search`. The store's name-only search and `API-05` are untouched.
- Real product photography: every image is a local placeholder.

## Risks

- **The diff is large and mostly data.** 140 rows plus the loader, the domain record and the view.
  The generator is seeded so a regeneration is a no-op; a reviewer should read the generator's rules,
  not 140 rows.
- **The featured count is asserted by name.** `CatalogFileTests` names the two featured products;
  the new catalogue must keep exactly two and the test names them.
- **The browser suite is count-sensitive.** Pages that list the catalogue were written for 6–10 per
  slot; every such assertion is in scope, and the failure will look like a UI failure when it is a
  data change.
- **Token growth.** The agent's payload rises from about 2,900 to about 10,000 tokens once metadata
  is read. That is the intended trade (metadata is what makes criteria matchable) but it is a real
  change in cost per request and should be recorded in the verification note.
- **Fictional data.** The catalogue is generated, so it is consistent and reproducible rather than
  true. Names remain Balinese place names and specs are plausible, not verified.

## Acceptance criteria

- API-24 … API-31 pass, and API-29 passes without reading any specific row of the data file.
- The loader fails, naming the SKU and field, on blank tags, unknown keys and non-string values.
- `Search` semantics are unchanged; the store page behaves as before apart from the new products.
- The three baselines are green and `specs/verifications/e03s01-after.md` records the counts and the
  new payload size.
