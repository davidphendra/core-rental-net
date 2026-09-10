# E1 — Catalog (read-only, in-memory)

**Slice:** 1 · **Status:** ready · **ADR:** 0004, 0005, 0015

## Goal
A single in-process authority for what can be rented and at what monthly price,
loaded from `products.json`, with no persistence and no mutation of any kind.

## In scope
- Solution scaffold and build discipline: `global.json` (SDK 10.0.400),
  `Directory.Build.props` (nullable, warnings-as-errors, `AnalysisLevel=latest`),
  `Directory.Packages.props` (exact pins, transitive pinning),
  `.editorconfig`, `.config/dotnet-tools.json` (`dotnet-ef` 10.0.12).
- `BuildingBlocks.Domain`: `Entity`, `AggregateRoot`, `ValueObject`, **`Money`**,
  `StronglyTypedId`, `Guard`, domain exception.
- `BuildingBlocks.Application` / `Infrastructure`: error types, id generator,
  `ModuleDbContext` base with the table-prefix convention.
- Catalog `Domain` / `Application` / `Infrastructure`: SKU, category,
  subcategory, monthly price, badge, image path; loading and validation of the
  file; queries by category, subcategory, SKU and featured; extras/accessories
  groupings; the `IDefineProductPrices` public contract.
- Tests: `Catalog.UnitTests`, `ArchitectureTests`.

## Out of scope
Persistence, seeding, migrations, upsert, admin, availability, stock.

## Stories
1. As a developer I want a build that fails on warnings and pins every package,
   so a vulnerable transitive cannot appear silently.
2. As the domain I want `Money` to carry an IDR amount with one rounding point,
   so totals cannot drift.
3. As the app I want `products.json` loaded and validated at startup and to fail
   fast with a clear message when it is malformed or missing.
4. As a page I want to query the catalog by category and subcategory.
5. As the Home page I want the two `badge: popular` SKUs for its featured cards.
6. As the Workspace I want a price for a SKU, so it can compute a quote.
7. As an architect I want the boundaries enforced by tests rather than review.

## Definition of done
Build clean with warnings-as-errors; `Catalog.UnitTests` and
`ArchitectureTests` green; matrix rows below marked passing; capsule archived.

## Matrix rows
CAT-01…CAT-13 · MON-01…MON-08 · ARC-01…ARC-06

## Risks
- 57 catalog image files do not exist → the UI must render a placeholder state
  rather than a broken image (handled in E3).
- 6 images are hosted on a third-party CDN and must be vendored (E3).
