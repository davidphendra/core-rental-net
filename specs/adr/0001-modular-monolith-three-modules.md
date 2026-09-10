# ADR-0001: Modular monolith with three modules

- **Status:** Accepted (2026-09-10)
- **Decided by:** product owner

## Context
The app is a demonstration-grade rental funnel. A referenced architecture
(kgrzybek/modular-monolith-with-ddd) was requested for structure. The scope was
narrowed during grilling: guest funnel only — no authentication, no admin, no
back office.

## Decision
A modular monolith with exactly three modules:

| Module | Owns | Does not own |
|---|---|---|
| `Catalog` | Products, subcategories, monthly prices (read-only) | Any persistence |
| `Workspace` | The draft, slot assignments, quantities, delivery address, quote | Orders, money owed |
| `Rentals` | Rental, invoices, periods, cancellation, order access tokens | Catalog prices, slot rules |

Each module is `Domain` / `Application` / `Infrastructure`. Cross-module access
happens only through public Application contracts. No module references another
module's `Domain` or `Infrastructure`, enforced by arch tests, not convention.

## Consequences
- Adding Identity, Delivery or Payments later is additive.
- No `IntegrationEvents`, no outbox, no event bus, no mediator: with three
  synchronous in-process modules they would be ceremony.
- One `DbContext` per persisting module (Workspace, Rentals). Catalog has none.
