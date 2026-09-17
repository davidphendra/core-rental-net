# e03s02 — a machine caller reads only the fields it needs

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| NONBROWSER | 558 passed, 0 failed | **564 passed, 0 failed** |
| BROWSER | 104 passed, 0 failed, 1 skipped | **unchanged** — the projection is an HTTP concern; the pages read the module in process |

## What changed

- `CatalogProjection` — the wire vocabulary: `full` (default) and `compact`. Bound by the catalogue's
  own binder, so an unknown word is refused with the words that would have worked, exactly as an
  unknown filter is.
- `CompactCatalogItem` / `CompactCatalogCollection` / `CompactCatalogProjection` — the compact shape,
  derived from `ProductView` rather than listed by hand from the module.
- `CatalogController` — takes `view`, branches on it, and reads the envelope's currency from the
  catalogue rather than from the rows that matched.
- `CatalogProjectionDescriptionTransformer` — carries the vocabulary into the document.

## Measured effect

| | Before | After |
|---|---|---|
| Fields per product, compact | 9 (`ProductView`) | **7** |
| Payload share dropped | — | `imagePath` 23 %, `imageAvailable` + `isFeatured` 11 % |
| Full answer | unchanged | unchanged, still the default |

## What the run corrected

1. **The document cannot carry two `200` schemas.** The plan was to declare both responses. Measured
   on the first run: the second `[ProducesResponseType]` for the same status and content type
   replaced the first, so the document described the *compact* envelope as the default and the
   existing shape test failed. The vocabulary now travels on the `view` parameter, added by an
   operation transformer, because this solution builds with `GenerateDocumentationFile` off and XML
   docs never reach the document. **The limitation is real and recorded:** a generated client will not
   model the compact body.
2. **`remarks` do not become a description.** The first attempt documented the projection in the
   action's remarks; the operation carried no description at all, for the same build-setting reason.
3. **The currency must come from the catalogue, not from the answer.** A filter that matches nothing
   still has to say what its prices would have been in, which is why an empty compact answer is
   asserted separately.

## Not done, deliberately

- **No second envelope on `ApiCollection`.** A nullable `currency` would have put the field in the
  full answer too, whose shape is asserted property by property. The compact answer has its own
  envelope.
- **No `view` in the call log.** The line records caller, filters and count, which is the contract
  `e01s03` published; adding a field to it is a change to that contract, not a detail of this story.
- **No cached or pre-computed projection.** The catalogue is 140 rows in memory; the projection is a
  `Select` per request, and the measurement that would justify caching does not exist.
