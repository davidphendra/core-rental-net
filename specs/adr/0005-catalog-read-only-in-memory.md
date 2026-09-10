# ADR-0005: Catalog is a read-only in-memory snapshot

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner

## Context
`src/shared/data/products.json` held 63 SKUs. The product owner removed the
partner item, leaving 62 (chair 10, desk 10, accessory 42 across beanbag 10,
coffee 10, lamp 6, monitor 8, plant 8).

## Decision
The Catalog has **no table, no migration, no seeding, and no add/update/delete
of any kind**. `products.json` is loaded once at startup into an immutable
in-memory snapshot behind `IProductCatalog`, path from configuration, copied to
build output, validated at startup (malformed file fails fast).

## Consequences
- Removes an entire `DbContext`, migration set, seeder and table-prefix arch
  test for Catalog. EF Core's SQLite package is not referenced by
  `Catalog.Infrastructure`.
- The `.db` file contains only `Workspace_*` and `Rentals_*` tables.
- Editing `products.json` after startup has no effect until restart. This is
  acceptable because the file is the source of truth and the DB is disposable.
- There is **no** availability, stock, retired or inactive concept anywhere.
  Every SKU present in the file is rentable. A single defensive guard prevents
  an unresolvable SKU reference from throwing an unhandled exception.
