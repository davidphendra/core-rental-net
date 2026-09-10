# ADR-0002: SQLite single file, module-isolated by table prefix

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner

## Context
Deployment target is Azure App Service with no managed database (cost
constraint). SQLite is a file. The reference architecture isolates modules by
database schema.

## Decision
One SQLite file, one connection string, module isolation by **table-name
prefix**: `Workspace_*`, `Rentals_*`. An arch test asserts every mapped entity's
table name carries its module prefix.

## Verified facts behind this
Measured on this machine with EF Core 10 + SQLite, not assumed:
1. `HasDefaultSchema("catalog")` is **silently ignored** — the DDL emitted
   `CREATE TABLE "Product"` with no schema prefix. Schema-per-module does not
   port, and fails silently.
2. `decimal` columns are stored as **TEXT**, but EF registers an `EF_DECIMAL`
   collation and an `ef_compare()` function, so `ORDER BY` and comparisons are
   correct *through EF*. Raw SQL or external tools sorting that column sort
   lexically and wrongly.
3. `IsRowVersion()` produces a nullable `BLOB` that EF never populates — a
   silent no-op. See ADR-0003.

## Consequences
- Backup, migration and reset are single-file operations.
- Money reads must always go through EF's translator. No `FromSqlRaw` price
  ordering, no direct sqlite3 reporting.
- Dev path `App_Data/corerental.db`, gitignored, disposable.
- Azure deployment is deferred (ADR-0014) but the path and journal mode remain
  configuration-driven so it stays a config change, not a rewrite.
