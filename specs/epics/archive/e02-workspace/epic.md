# E2 — Workspace (the draft)

**Slice:** 1 · **Status:** ready · **ADR:** 0003, 0006, 0007, 0008

## Goal
A server-authoritative draft: the customer's slot composition, priced from the
catalog, persisted in SQLite and addressed by an opaque cookie token.

## In scope
- `Workspace` aggregate: slot assignments, quantities, delivery address,
  `Version` concurrency token, hashed `DraftToken`, `Draft → Converted` state.
- Slot rule table in **data**: Desk 1, Chair 1, Monitor **3**, Lamp 1, Plant 1,
  Coffee 1, Relax 1. Category compatibility and capacity invariants.
- Quote computation from the catalog — references only, never stored prices.
- Persistence: `WorkspaceContext`, `Workspace_*` tables, one migration, EF
  configuration for `Money` and the concurrency token.
- Application commands/queries: assign, remove, change quantity, set address,
  get current draft, get quote; `IDefineWorkspaceComposition` public contract
  returning a frozen composition for Rentals.
- Host plumbing for the token: HTTP middleware issues and rotates it; a scoped
  provider captures it during prerender.
- Tests: `Workspace.UnitTests`, integration tests against a real SQLite file.

## Out of scope
Orders, invoices, money owed, any UI beyond what E3 consumes.

## Stories
1. As a customer I want to assign a product and have it land in the right slot
   without choosing the slot myself.
2. As a customer I want a second monitor to raise the quantity rather than
   replace the first, up to three.
3. As a customer I want a fourth monitor to be refused with a clear message.
4. As a customer I want a `×` to remove an item, and steppers to change quantity.
5. As a customer I want my workspace to survive a reload and a second tab.
6. As the business I want the quote computed on the server from the catalog, so
   no client can dictate a price.
7. As Rentals I want a frozen composition, so I never read Workspace tables.

## Definition of done
Build clean; unit + integration tests green; matrix rows passing; capsule archived.

## Matrix rows
SLOT-01…SLOT-05 · WS-01…WS-14 · DR-01…DR-07 · SEC-05

## Risks
- Draft token lifetime is an HTTP concern, not a component concern: a Blazor
  circuit **cannot set a cookie**. This is the highest-risk piece of plumbing in
  the project and is isolated in the host.
- `IHttpContextAccessor` is unreliable inside a circuit scope; the token must be
  captured during prerender.
