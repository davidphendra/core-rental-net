# CQRS design — CoreRentalNet

Scope: the whole solution (`src/`). The record was produced with the `design-cqrs` skill against
[its rule catalog](../../../.pi/agent/skills/design-cqrs/REFERENCE.md). Date: 2026-09-16.

## Context and demand

CoreRentalNet is a single-deployable rental application: a Blazor host over three business modules
(Catalog, Rentals, Workspace) and one SQLite database file. The business is a task-based builder:
a customer starts a draft, fills seven zones on a canvas, sets a delivery address, and turns the
draft into a rental order.

| Profile | Current shape |
|---|---|
| Write side | `AssignProduct`, `RemoveAssignment`, `ChangeQuantity`, `SetDeliveryAddress`, `StartDraft`, `Checkout`, `ConvertWorkspaceToOrder`. Each one is a command record plus a handler. |
| Write rules | Slot capacity, single-unit replacement, address bounds (5–200 characters), the terminal state of a converted draft, the demonstration confirmation gate, catalog re-pricing at checkout, one rental per workspace. |
| Read side | `GetWorkspace`, `GetWorkspaceQuote`, `GetRentalByToken`, `GetInvoicesByToken`, `GetFeaturedProducts`, `GetProductBySku`, `SearchCatalog`. Each returns a view record. |
| Store | One SQLite file, three `DbContext`s, one per module. |
| Consumer | The Blazor circuit, through `IWorkspaceSession` and the module handlers. |

The measured pain, in one sentence: **the write handlers return the read view**. `AssignProductHandler`,
`RemoveAssignmentHandler`, `ChangeQuantityHandler`, `SetDeliveryAddressHandler` and
`StartDraftHandler` each mutate the stored draft *and* return `WorkspaceView`, so the write side
carries a read projection and the read shape changes whenever the canvas changes.

The read side is not under load: this is a single-instance SQLite application with no reporting
workload and no search engine. There is no measured read and write asymmetry.

## Level decision

**Chosen level: L1 — separate read and write models over one store.**

| Axis | Verdict | Evidence |
|---|---|---|
| A — model segregation | **Yes.** | The write side carries real business rules (slot capacity, terminal state, address bounds, the confirmation gate). The read side must not. It already returns a different type, `WorkspaceView`, built by `IWorkspaceViewService`. |
| B — store segregation | **No.** | One SQLite file is the whole datastore. There is no second store, no schema split, no scale profile, and no measured read/write asymmetry. |

Rejected higher levels, each with its reason:

| Rejected | Reason |
|---|---|
| L2 — separate read and write stores | No demand. There is no measured read/write asymmetry and no reporting or search workload. Taking L2 would add an outbox, idempotent consumers, projection lag and its monitoring for no measured benefit (MS-CQRS). |
| L3 — L2 plus event sourcing | No demand. History is not the product; the rental and invoice records already carry the facts the business reads. Event sourcing is orthogonal and would be adopted separately (MS-CQRS). |

Axis A is the Microsoft "simplest CQRS": one database, two models. That is what this repository
already is, with one exception — the exception is the finding below.

### The finding this design fixes

The five workspace command handlers violate the method-level rule: a method returns state or
mutates state, never both (CLEANCODE chapter 3; MEYER; MS-CQS). `HandleAsync` mutates the stored
draft and returns `WorkspaceView`.

**The fix:** a command handler returns `Task` and nothing else. The Blazor session re-reads through
`IGetWorkspaceHandler` after the write succeeds. The write side no longer knows the read shape, and
`WorkspaceView` stops being a contract of the write path.

`CheckoutCommandHandler` and `IPlaceOrder` are **not** part of this finding. They return
`CheckoutResult` / `PlaceOrderResult`, which carry the identity and confirmation of the thing that
was just created (rental number, access token, invoice number, total). That is a command result,
not a read projection, and the caller cannot re-derive it because the raw access token is returned
exactly once. Keeping it is deliberate.

## Information flow

### Business flow

```
customer fills the canvas
      |
      v  business intent, in business vocabulary
  COMMAND  AssignProduct / RemoveAssignment / ChangeQuantity / SetDeliveryAddress / StartDraft
      |
      v  write-side business rules  (IWorkspaceService: slot capacity, terminal state, address bounds)
  write model  (the stored draft record; IWorkspaceRepository)
      |
      v  the business fact
  state change  (draft saved, Version bumped)
      |
      v
  BUSINESS VIEWS  (the builder canvas, the quote panel, the review page)
      ^
      |  business question, no side effect
  QUERY  GetWorkspace / GetWorkspaceQuote
```

```
customer rents the workspace
      |
      v  Checkout  (confirmation phrase, address, catalog re-pricing)
  write model  (draft marked terminal, rental and first invoice written)
      |
      v
  BUSINESS VIEWS  (confirmation page; "my rentals" / "my invoices" behind the access token)
      ^
      |  business question, no side effect
  QUERY  GetRentalByToken / GetInvoicesByToken
```

Decision points:

- One command is one business task and one transaction. `Checkout` is one command; it coordinates
  the conversion and the order placement rather than fanning out into a workflow.
- A state change is the business fact. There is no second store to publish it to, so there is no
  integration event and no outbox at L1.
- A read model maps to a stakeholder question. `WorkspaceView` answers "what is on my canvas and
  can I leave?"; `WorkspaceQuote` answers "what will a month cost?"; `RentalView` answers "what did
  I rent?"; `InvoiceView` answers "what do I owe?".
- Read-store staleness is zero by construction: the read and the write share one store and one
  transaction.

### Technical flow

```
WRITE PATH
Blazor component
  -> IWorkspaceSession.AssignAsync
  -> IAssignProductHandler.HandleAsync(AssignProductCommand)     [use case, one operation]
       -> IWorkspaceService.Assign                                 [business rules]
       -> IWorkspaceRepository.SaveChangesAsync (ONE transaction)
  -> Task returned; no view
  -> IWorkspaceSession re-reads through IGetWorkspaceHandler
  -> WorkspaceView rendered

READ PATH
Blazor component
  -> IWorkspaceSession.RefreshAsync / IGetWorkspaceHandler
  -> GetWorkspaceQuery                                    [use case]
  -> IWorkspaceResolver -> IWorkspaceRepository.FindByTokenAsync
  -> IWorkspaceViewService.Build -> WorkspaceView          [projection]
  -> rendered
```

Boundaries:

1. **Transaction boundary equals command boundary.** Each handler opens one unit of work and saves
   once. `RemoveAssignment` deliberately saves nothing when the slot was already empty.
2. **Sync or async contract.** Synchronous request/response. There is no `202 Accepted`, no poll and
   no notification, because there is no asynchrony to hide at L1.
3. **Delivery guarantee, deduplication, ordering key.** Not applicable at L1 — no broker, no
   consumer, no replay. Where duplication is possible (`Checkout` retried), the handler already
   reads back the existing rental by workspace id and returns it instead of writing a second one;
   that is the deduplication.
4. **Read-store freshness baseline.** Immediate. A read after a write sees it, because both use the
   same SQLite file and the write commits before the response.

## Complexity ledger

Every cost this design accepts, ranked by real-world damage.

| # | Cost | Why it is accepted |
|---|---|---|
| 1 | Duplicated models and mapping drift | The read port must be kept honest by `IWorkspaceViewService`; there is no second store to make it authoritative. Drift is caught by the view and quote unit tests. |
| 2 | Loss of ORM scaffolding on the read side | The read path loads the stored draft and projects it in memory rather than issuing a hand-written read query. Accepted: the dataset is one draft per browser, and SQLite is in-process. |
| 3 | Cognitive cost of two paths per feature | One query handler and one command handler per task. Accepted: the alternative is a write handler that owns a view. |
| 4 | An extra round trip after each write | The session re-reads after a write instead of reusing a returned view. Accepted: it is one in-process `FindByTokenAsync` over SQLite. |
| 5 | No dispatcher abstraction | Handlers are injected directly and called through their interfaces. Accepted: a media-tor library is a dispatch choice, not the pattern (ARD), and no handler needs pipeline behaviour. |

No cost from the ledger's expensive end is accepted: there is no eventual consistency, no outbox,
no projection monitoring and no distributed-data correctness work, because the store is not split.

## .NET wiring

| Concern | Decision | Placement |
|---|---|---|
| Command records | Immutable `sealed record`, one per file | `*.Application/Commands/<Operation>/…Command.cs` |
| Command handlers | One public operation, returns `Task`; injected by interface | `*.Application/Commands/<Operation>/…Handler.cs` |
| Query records | Immutable `sealed record` | `*.Application/Queries/<Operation>/…Query.cs` |
| Query handlers | Return a view record, never mutate | `*.Application/Queries/<Operation>/…Handler.cs` |
| Read models | The views a query returns | `*.Application/Queries/Views/` |
| Ports | `I…` declared in the inner layer, `sealed` implementation outward | `Domain/Persistence`, `Application/**` |
| Services | Rule, domain and projection services alike; never nested | `*.Application/Services/` |
| Store | EF Core over SQLite, one `DbContext` per module | `*.Infrastructure` |
| Dispatcher | None. Direct interface injection through DI. | `Host/Composition` |
| Lifetimes | Handlers and services `Scoped`; the session holds the circuit's cache | `Host/Composition/*Registration.cs` |

Placement rule for the use-case folders: `Commands/` and `Queries/` hold use cases only — a
message, its port, its handler and the result or view it returns. Anything that is not a use case
(a rule, a policy, a projection, a domain service) goes to `Rules/` or the single top-level
`Services/`; there is no `Commands/Services/` and no `Queries/Services/`.

Wiring rule enforced by the change: **a command handler's interface returns `Task`; a query
handler's interface returns a view record.** `IAssignProductHandler`, `IRemoveAssignmentHandler`,
`IChangeQuantityHandler`, `ISetDeliveryAddressHandler` and `IStartDraftHandler` are changed to match.

## Sources

| Tag | URL |
|---|---|
| MS-CQRS | https://learn.microsoft.com/azure/architecture/patterns/cqrs |
| MS-CQS | https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns |
| MS-READS | https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/cqrs-microservice-reads |
| CLEANCODE | *Clean Code*, chapter 3 — CQS as a function-level rule |
| MEYER | Bertrand Meyer, *Object-Oriented Software Construction* — CQS |
| ARD | https://github.com/ardalis/CleanArchitecture |
