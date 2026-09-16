# Application grouping by CQRS role — AFTER

The three Application projects were regrouped by the role each file plays: use cases under
`Commands/` and `Queries/`, everything that is not a use case under `Rules/`, `Services/`,
`Contracts/`, `Scheduling/` or `Support/`. Namespaces follow their folders in every project.

A pure move: no type, member or signature changed, so every count is unchanged.

| Baseline | Before | After | Result |
|---|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors | unchanged |
| NONBROWSER | 492 passed, 0 failed | 492 passed, 0 failed | unchanged |
| BROWSER | 103 passed, 1 skipped | 103 passed, 1 skipped | unchanged |

## Rule applied

- A **Command** is a write use case: message, port, handler, and the result it returns.
- A **Query** is a read use case: message, port, handler, and the view it returns.
- `Commands/` and `Queries/` hold **use cases only**.
- Everything else — rules, policies, projections, domain services, configuration — is not a use
  case, and goes to `Rules/`, a single top-level `Services/`, `Scheduling/`, `Contracts/` or the
  project root. There is no `Commands/Services/` and no `Queries/Services/`.

## Catalog.Application

`GetProductByFeature`, `GetProductBySku` and `SearchCatalog` are all reads → `Queries/`. There are
no commands: the module is read-only.

```
Catalog.Application/
├─ Contracts/                (unchanged)
└─ Queries/
    ├─ GetProductByFeature/
    ├─ GetProductBySku/
    └─ SearchCatalog/
```

## Rentals.Application

| Was | Now | Role |
|---|---|---|
| `Checkout/CheckoutCommand`, `ICheckoutCommandHandler`, `CheckoutCommandHandler`, `CheckoutResult` | `Commands/Checkout/` | Command |
| `Orders/` (5 files) | `Commands/PlaceOrder/` | Command (handler named `PlaceOrderService`) |
| `Queries/GetRentalByToken/`, `Queries/GetInvoicesByToken/` | unchanged | Query |
| `Views/` (4 files) | `Queries/Views/` | Query read models |
| `Checkout/ICheckoutConfirmation`, `CheckoutConfirmation` | `Services/` | projection over a stored order |
| `Checkout/DemoConfirmation` | `Rules/` | the typed-acknowledgement rule |
| `Invoicing/IInvoiceService`, `InvoiceService` | `Services/` | domain service |
| `Rentals/RentalLifecycleService` (+ interface) | `Services/` | write-side rule service |
| `Rentals/*PolicyService` (6 files) | `Rules/` | policies |
| `Scheduling/` (7 files) | unchanged | time-driven write process, not a request use case |
| `RentalsSettings.cs` | unchanged | configuration |

```
Rentals.Application/
├─ Commands/Checkout|PlaceOrder
├─ Queries/GetInvoicesByToken|GetRentalByToken|Views
├─ Rules/       policies + DemoConfirmation
├─ Scheduling/  time-driven process
├─ Services/    InvoiceService, RentalLifecycleService, CheckoutConfirmation
└─ RentalsSettings.cs
```

## Workspace.Application

The projection services moved from `Queries/Services/` to the single top-level `Services/`, joining
`WorkspaceService`, so all three projects follow one rule.

```
Workspace.Application/
├─ Commands/<Operation>/   5 × { message, I…, Handler }
├─ Queries/GetWorkspace|GetWorkspaceQuote|Views
├─ Rules/  Services/  Support/  Contracts/
```

## What changed

- Catalog: 3 folders moved; Rentals: 26 files moved across 6 destinations; Workspace: 6 files moved.
- Every namespace declaration was rewritten to match its folder (41 rewrites), and every reference
  across `src` and `tests` updated, including `_Imports.razor`.
- `specs/CQRS_DESIGN_LATEST.md` ".NET wiring" table updated to the new placement rule.
