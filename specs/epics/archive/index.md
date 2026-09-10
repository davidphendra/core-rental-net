# Epic archive

An epic lands here automatically once it builds clean and its tests pass
(ADR-0014). **Archived means "mechanical criteria met", not "accepted".** The
product owner is the sole authority on completion; if an archived epic fails
review it is moved back to `specs/epics/`.

Each entry records the evidence, so a finished-looking epic still shows exactly
what remains the product owner's call.

| epic | completed | commit | tests | deliberately not done |
|---|---|---|---|---|
| **E1 catalog** | 2026-09-10 | `8b29e8f` | 69 passing — 46 unit, 12 integration against the real catalog file, 11 architecture | BuildingBlocks.Application/Infrastructure (errors, id generator, `ModuleDbContext`, SQLite setup) deferred to E2 where they have consumers and tests. Domain base types (`Entity`, `AggregateRoot`, `ValueObject`, `StronglyTypedId`) deferred to their first consumer. ARC-01/02/03 pass vacuously until E2 adds a DbContext and a second module. |
| **E2 workspace** | 2026-09-10 | `e5aa676` | 140 passing — 99 unit, 30 integration against a real SQLite file per test, 11 architecture | HTTP cookie middleware deferred to E3 (no request pipeline exists yet), so DR-01…DR-04 and DR-07 stay pending. `ModuleDbContext` deliberately not created: explicit prefixed table names plus the source-scan rule, one mechanism rather than two. The published catalog-to-slot mapping is proven total over the real 62-product file, and every slot is proven reachable. WS-04/06/08/10 are unit-proven; their E2E half lands in E7. |
