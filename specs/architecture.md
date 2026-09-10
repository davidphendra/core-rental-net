# Core Rental — Architecture

Month-to-month rental of fully-equipped office setups to digital nomads and
startups in Bali. A customer composes a workspace from a fixed catalog, reviews
it, and rents it.

## 1. Posture

- **Deployment:** local development and CI only. Azure App Service is the
  intended target and is **deferred** (ADR-0014). Nothing in the code blocks it;
  see §7.
- **Brand:** Core Rental.
- **Access:** anonymous. There is no login, no account, and no admin surface
  (ADR-0009). The agent never deploys or provisions anything without explicit
  consent (ADR-0014).

## 2. Module map

```
src/
  BuildingBlocks/{Domain,Application,Infrastructure}
  Modules/
    Catalog/{Domain,Application,Infrastructure}     # read-only, no persistence
    Workspace/{Domain,Application,Infrastructure}   # the draft
    Rentals/{Domain,Application,Infrastructure}     # orders, invoices, periods
  Host/CoreRentalNet.Host                           # Blazor composition root
tests/
  CoreRentalNet.Modules.{Catalog,Workspace,Rentals}.UnitTests
  CoreRentalNet.ArchitectureTests
  CoreRentalNet.IntegrationTests
  CoreRentalNet.E2E
  CoreRentalNet.TestSupport
```

**Boundary rules (enforced by arch tests, not convention):**
1. No module references another module's `Domain` or `Infrastructure`.
2. Cross-module calls go through public `Application` contracts
   (`IDefineProductPrices`, `IDefineWorkspaceComposition`).
3. One `DbContext` per persisting module; a module never touches another's tables.
4. Every mapped entity's table name carries its module prefix.
5. `Domain` references no EF Core or ASP.NET type.

There is no mediator, no integration-event bus and no outbox: with three
synchronous in-process modules they would be pure indirection (ADR-0001).

## 3. Domain responsibilities

| Module | Aggregate | Owns |
|---|---|---|
| Catalog | — (immutable snapshot) | SKU, name, category, subcategory, monthly price, badge, image path |
| Workspace | `Workspace` (the draft) | slot assignments, quantities, delivery address, `Version`, `DraftToken` (hashed), `Draft → Converted` state |
| Rentals | `Rental`, `Invoice` | order number, access token (hashed), status, contact-free order detail, delivery address snapshot, period anchor, cancellation, invoice lines and snapshotted amounts |

**Slot rules (ADR-0008):** Desk 1 · Chair 1 · **Monitor 3** · Lamp 1 · Plant 1 ·
Coffee Station 1 · Relax Zone 1. Maximum 9 assignable units. Selection replaces;
exceeding capacity is refused with a message. `Outdoor Gear` and `Garage` are
gone; zones are derived from the subcategories actually present in the catalog.

**Money (ADR-0004):** IDR only. `Money(decimal Amount, string Currency)`,
`decimal(18,2)`, one rounding point with `MidpointRounding.AwayFromZero`, display
`id-ID` at 0 decimals. Tax modelled at a 0% seed; the line renders only when
non-zero.

## 4. Pricing and trust

The draft stores **references and quantities only**. Every total is recomputed
from the Catalog snapshot on read; amounts are **snapshotted at invoice issue**.
The client never sends a price, total or rate that influences a charge
(ADR-0006). The displayed total and the charged total come from the same code.

## 5. Request flow

```
GET /            → middleware issues draft token cookie (if absent)
GET /builder     → assign/remove items via circuit events; total recomputed
GET /extras      → catalog extras; "add to setup" assigns to its slot
GET /review      → quote + delivery address (2-line textarea, 5–200 chars)
                   redirects to /builder if the workspace is empty
[Rent This Setup]→ dialog → user types "this is a demo" (trimmed, case-insensitive,
                   validated client AND server) → order persisted, first Invoice
                   raised and settled, draft cleared, token rotated
GET /orders/{number}?t=…  → confirmation, reachable only with the access token
```

Disabled while the workspace is empty: mobile `Summary`, mobile `Rent`,
side-panel `View Setup Summary`, floating `Ready to Rent?`, and the Review CTA.
Product assignment stays enabled. The header bag icon is decorative (ADR-0012).
The checkout command rejects server-side regardless of UI state.

## 6. Scheduler

A hosted service driven by an injected `TimeProvider` (ADR-0011): schedules
delivery at a fixed lead time, activates on that date, and on each monthly
anniversary raises and immediately settles the next invoice. Cancellation
suppresses the next invoice and ends the rental at the end of the paid period.
Period *N* = `anchor.AddMonths(N)`, so a 31st order clamps to 28/30 Feb and
returns to the 31st later. Business dates derive at a fixed `+08:00` (WITA);
there is no daylight saving in Indonesia, so no timezone database is involved.

## 7. Persistence constraints (measured, not assumed)

One SQLite file, module isolation by table prefix (ADR-0002):

1. `HasDefaultSchema()` is **silently ignored** by the SQLite provider — verified.
2. `decimal` is stored as **TEXT**; EF registers an `EF_DECIMAL` collation plus
   `ef_compare()`, so ordering and comparison are correct **through EF only**.
   Raw SQL or external tools sort money lexically and wrongly.
3. `IsRowVersion()` is a **silent no-op** — a nullable BLOB EF never fills.
   Concurrency uses an explicit `int Version` (ADR-0003).
4. `PRAGMA journal_mode=WAL` + `busy_timeout` locally. **WAL does not work over a
   network filesystem** (SQLite docs), and App Service's only persistent writable
   storage is a UNC share — so WAL is local/CI only and `Sqlite__JournalMode`
   must fall back to a rollback journal on App Service.
5. SQLite locking over a network filesystem is hazardous: the app must **never
   scale out**. One instance, always.
6. Migrations are incremental and applied at startup in Development only;
   seeding never runs against a non-empty database.

## 8. Security rules

- No card data, no payment provider, no secrets in the repository.
- The client never influences an amount that is charged.
- Draft and order access are token-based; both tokens are stored **hashed** and
  are opaque 128-bit values, not identifiers.
- The demo phrase is validated on the server, not only in the browser.
- A disabled control is a courtesy, never a boundary.
- An unresolvable SKU reference returns a validation error, never a 500.

## 9. Testing

Unit (xUnit v2 + AwesomeAssertions, no mocking library) · Integration (real
SQLite file per test) · Architecture (NetArchTest) · E2E (real Kestrel subprocess,
per-run throwaway database, Playwright over a real socket, **zero interception**).
`specs/test-matrix.md` is the source of every scenario (ADR-0013).

## 10. Out of scope

Identity provider · admin/back office · payments and Stripe · partner services
(bike rental) · stock/availability · deposits · proration · commitment terms ·
multi-currency · i18n · bulk clear-workspace · bUnit component tests · axe gate ·
deployment automation · email notifications · drag-and-drop canvas.
