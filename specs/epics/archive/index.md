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
