# Rename remainder — the `catalogService` / `similarityService` corruption

**Status:** open, tracked
**Date:** 2026-09-22
**Origin:** a bulk rename replaced the substring `catalog` with `catalogService` (and `similarity` with
`similarityService`) across source, tests and prose. The load-bearing strings were repaired in the baseline
repair; this note tracks what is left, so it is a known debt rather than a forgotten one.

## What was repaired (load-bearing)

- The routes: `/api/catalog` and `/api/catalog/similarity` (`CatalogRoutes`), and every test URL that must match
  them.
- The error codes: `catalog.unknown_filter`, `catalog.similarity_unavailable`, `catalog.unauthenticated`,
  `catalog.not_permitted` (`ApiErrorCode`).
- The claim value `read:catalog` everywhere it is configured or asserted.
- The `AuthorizationGateTests` literal, which the rename had itself corrupted — the test was guarding a string
  nobody used.
- `ApiErrorCode.RunInFlight` and the Host's dead references to the Foundry/Azure SDKs.

## What is left — and how to tell corruption from correctness

**Not every `catalogService` is wrong.** `IProductCatalogService` is a real type, and `catalogService` is its
idiomatic camelCase parameter (`SearchCatalogHandler(IProductCatalogService catalogService)`). Those are **not**
corruption and must be left alone. A blind replace would break them.

The remaining **90** `catalogService` and **18** `similarityService` occurrences fall into:

### 1. User-visible strings — fix first (these reach a customer or a caller)

| File | What it says |
|---|---|
| `src/Host/.../Components/Shared/SlotPickerDialog.razor:85` | `"Nothing in the catalogService fits this slot."` |
| `src/Host/.../Components/Pages/Review.razor:90` | `"An item in your workspace is no longer in the catalogService."` |
| `src/Host/.../Components/Shared/AccessRefused.razor:9` | `"…it does not carry catalogService access."` |
| `src/Host/.../Infrastructure/ProductSimilarityUnavailableHandler.cs:46` | `Title = "The catalogue cannot be searched by similarityService."` |
| `src/Modules/Workspace/.../AssignProductHandler.cs:24` | `"The catalogService does not know the product '…'."` |
| `src/Modules/Catalog/.../Loader/ProductFile.cs:22` | `"No catalogService file path was configured."` |
| `src/Modules/Rentals/.../CheckoutCommandHandler.cs:148` | `"'…' is no longer in the catalogService."` |
| `src/Host/.../Presentation/SlotCatalog.cs:34` | `"No catalogService query answers for this slot."` |
| `src/Host/.../Presentation/CatalogTabs.cs:31` | `"Unknown catalogService tab."` |

### 2. Mangled words — the rename turned `catalogue` into `catalogServiceue`

| File | Text |
|---|---|
| `src/Host/.../Components/Pages/Home.razor:9` | `"real catalogServiceue names and prices"` |
| `src/Host/.../Components/Shared/SlotView.razor:19` | `"The catalogServiceue still shows one"` |

### 3. Prose and comments — the bulk

Roughly eighty occurrences across ~55 files, where a comment says *"the catalogService"*, *"a similarityService
search"*, *"asks the catalogService for its own kind of thing"*. Representative files (not exhaustive):

`src/Host/.../Extentions/{CatalogRegistrationExtentions,ApplicationPipelineExtentions,ApiResponseRegistrationExtentions,AuthorizationRegistrationExtentions,GuardedProductSimilarityService}.cs`,
`src/Host/.../Infrastructure/{CatalogPolicy,SimilaritySearchPolicy,ProductSimilarityUnavailableHandler}.cs`,
`src/Host/.../Presentation/{CatalogBrowser,CatalogTabs,CatalogTab,SlotCatalog,RoleClaims,AccessoryGroups,CheckoutGate}.cs`,
`src/Host/.../Components/**/*.razor`,
`src/Modules/Catalog/.../{SearchCatalogHandler,GetFeaturedProductsHandler,GetProductBySkuHandler,ProductViewMapper,ProductFile,ProductBadge,ProductLoadException}.cs`,
`src/Modules/Workspace/.../{WorkspaceQuoteService,AssignProductHandler}.cs`,
`src/Modules/Rentals/.../CheckoutCommandHandler.cs`,
and matching comments in `tests/**`.

### 4. Cosmetic namesakes

- Test fixture paths: `core-rental-catalogService-…`, `core-rental-api-similarityService-…`
  (`TemporaryCatalogFile.cs`, `CatalogApiSimilarityFactory.cs`).
- Sample values in tests: `"catalogService:read"` (`CatalogAuthorizationPolicyTests`, `IdentitySettingsTests`).
- Local identifiers: `nearestCatalogsVector` in `SearchSimilarityCatalogHandler`.

## Dead code and configuration the removal left behind

- **`AIUse` is no longer dead.** It is read again: the builder was restored, and `AiPolicy` guards the section and
  the endpoint with the claim (`Authorization:AIUse`). Its declaration in `src/Host/.../appsettings.json` and
  `Authorization__AIUse__*` in `tests/CoreRentalNet.E2E/HostFixture.cs` are load-bearing and must not be swept.
- **The E2E builder suite**: `tests/CoreRentalNet.E2E/Flows/AiBuilder*.cs` still drive `/api/builder/suggest`, and
  `HostFixture.StartLocalAgent` still starts the deleted `tests/CoreRentalNet.E2E.LocalAgent` project via
  `TestPaths.LocalAgentAssembly()`. The project is gone, so the BROWSER baseline is red. It is not in this
  change's definition of done (`specs/adr/0005`), but it must be removed or re-pointed before the browser tier can
  be trusted again.
- `tests/CoreRentalNet.E2E.LocalAgent/obj/` is an empty directory left by the deletion.
- `tests/CoreRentalNet.Host.Tests`'s reference to the deleted stand-in was removed in the baseline repair; the E2E
  project's is a runtime path, not a project reference, and was not.

## Why this is deferred rather than fixed now

The repository was mid-change (321 uncommitted files: the ingestion tool, the similarity endpoint, and the removal
of the builder and shortlist). A 108-occurrence sweep through work that is still in flight is the largest possible
diff with the least possible signal, and every mis-judged occurrence silently breaks a name. The load-bearing
strings were repaired so the tree builds, routes and passes; the rest is cosmetic **except the user-visible
strings above**, which should be fixed in the same change that next touches those files.
