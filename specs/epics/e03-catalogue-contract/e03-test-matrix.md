# e03 — Catalogue contract test matrix

Risk drives `verify-work` depth. Levels: `unit` = a test project without a host; `integration` =
against the real `src/shared/data/products.json`; `http` = in-process `WebApplicationFactory`
request; `browser` = the Playwright suite.

| ID | Story | Scenario | Level | Risk |
|----|-------|----------|-------|------|
| API-24 | e03s01 | The catalogue holds 140 products: 20 desks, 20 chairs, and 20 in each of the five accessory subcategories | integration | P0 |
| API-25 | e03s01 | Every product carries a metadata record whose tags are non-empty and whose attribute values are strings | integration | P0 |
| API-26 | e03s01 | Two products in the same slot differ in metadata, so a criterion can separate them | integration | P0 |
| API-27 | e03s01 | Metadata that is malformed - unknown key, non-string attribute value, blank tag - fails the load naming the SKU and the field | unit | P1 |
| API-28 | e03s01 | Exactly two products are featured, and a product outside the catalogue's vocabulary cannot be added | integration | P1 |
| API-29 | e03s01 | A remote image with a vendored copy resolves to the local file, and the real catalogue keeps some of them | integration + unit | P1 |
| API-30 | e03s01 | The published body carries metadata for every product | http | P0 |
| API-31 | e03s01 | The matching vocabulary is well formed: tags and attribute keys lower case, hyphenated, unique, non-blank | integration | P0 |
| API-32 | e03s02 | `view=compact` returns SKU, name, category, subcategory, price, description and metadata, and omits the image and display fields | http | P0 |
| API-33 | e03s02 | The compact envelope states the currency once, and a catalogue with more than one currency fails the load | http + unit | P0 |
| API-34 | e03s02 | An unknown `view` value is refused 400 naming the allowed values | http | P1 |
| API-35 | e03s02 | Non-vacuity: removing the projection fails API-32 | http | P1 |
| API-36 | e03s03 | The envelope reports `total` and `truncated`; with no limit the answer is complete and `truncated` is false | http | P0 |
| API-37 | e03s03 | With a limit below the number matched, `count` is less than `total` and `truncated` is true | http | P0 |
| API-38 | e03s03 | Non-vacuity: a response built without the truncation check fails API-37 | http | P1 |

## Baselines

Record the current numbers at kickoff rather than trusting a prior figure: the counts have moved
since `e01` was recorded. The requirement is BUILD 0 warnings 0 errors; NONBROWSER rises only by the
new tests and fails 0; BROWSER fails 0.

Expect **browser churn**: the store and the builder render the catalogue, and today's suite was
written against 62 products with 6 to 10 per slot. Every count-sensitive page assertion is in scope
for this story, and the store search stays name-only (`API-05`) by design.

## Non-goals in this matrix

- No scenario searches the catalogue by description. The store's name-only search is unchanged, and
  metadata matching belongs to the agent that reads the compact projection.
- No embedding, vector, or semantic-retrieval scenario: metadata is the deterministic substitute for
  it, and the trigger conditions for revisiting are recorded in the scope.
- No load or rate-limit scenario: one server-side caller and a bounded in-memory snapshot.
