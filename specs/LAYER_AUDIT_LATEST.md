# Layer audit — clean architecture

Skill: `audit-clean-architecture`, rule catalog
[`REFERENCE.md`](../../.pi/agent/skills/audit-clean-architecture/REFERENCE.md). Date: 2026-09-16.
Scope: the whole solution (`src/` and `tests/CoreRentalNet.ArchitectureTests`).

## Verdict

| Check | Command | Result |
|---|---|---|
| Layering, default | `check-layering.sh src` | **0 flags** |
| Layering, Catalog exempt | `LAYER_EXEMPT_MODULES=Catalog check-layering.sh src` | **0 flags** |
| Checker self-test | `check-layering.sh --self-test` | self-test OK |
| Build | `dotnet build CoreRentalNet.sln` | succeeded, 0 warnings, 0 errors |
| Non-browser tests (8 projects) | `dotnet test …` | **482 passed, 0 failed** |
| Architecture tests | `CoreRentalNet.ArchitectureTests` | **50 passed, 0 failed** |
| Browser tests (`CoreRentalNet.E2E`) | `dotnet test tests/CoreRentalNet.E2E` | **103 passed, 1 skipped, 0 failed** |

Every mechanical check is clean, so each finding below came from reading the lines against the rule
catalog.

## Findings

| ID | Severity | File:line | Rule | Source | Fix |
|---|---|---|---|---|---|
| L1 | MEDIUM | `Workspace.Application/Commands/**` (5 handlers, before this round) | A use case is thin and coordinates; `Core` and `Application` own the business shape, not the read shape | [ARCH](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures), [DDDMS](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice) | **Fixed this round.** The write handlers returned `WorkspaceView`, so the use-case output was the presentation read shape. They now return `Task`; the session re-reads through `IGetWorkspaceHandler`. |
| L2 | MEDIUM | `Host/Presentation/IWorkspaceSession.cs:2`, `SlotCatalog.cs:4`, `SlotCss.cs:1`, `ProductGlyph.cs:2`, `WorkspaceSession.cs:10`; `Host/Infrastructure/DraftTokenMiddleware.cs:2` | A business type must not travel to the presentation layer unchanged when the page needs another shape; translate instead | [DDDMS](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice), [PRIN](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles) | Publish the slot and draft-state vocabulary in `Workspace.Application.Contracts`, or promote it to `BuildingBlocks` if it is genuinely shared. Listed, not fixed: it is a MEDIUM and touches three layers. |
| L3 | LOW | `Host/Components/_Imports.razor:12,31` | A documented exception must carry a test that names the allowed namespace | [REFERENCE.md §G](../../.pi/agent/skills/audit-clean-architecture/REFERENCE.md) | `_Imports.razor` makes every module's Application and Domain namespace visible to every component. Trim to the contracts the components need, or add a test naming the Host's allowed namespaces. |
| L4 | LOW | `tests/CoreRentalNet.ArchitectureTests/` | Add an architecture test for each rule a human can forget | [REFERENCE.md §H](../../.pi/agent/skills/audit-clean-architecture/REFERENCE.md) | **Fixed this round.** `ApplicationDependencyTests` now asserts that no Application assembly names EF Core, SQLite or ASP.NET — the same door ARC-04 closes for Domain. Non-vacuous: it asserts four Application assemblies were discovered. |
| L5 | LOW | `Host/Presentation/CatalogBrowser.cs`; `Composition/CatalogRegistration.cs:29` | An interface is for a seam; a type with one implementation and a test seam may stay concrete | [REFERENCE.md §C LOW](../../.pi/agent/skills/audit-clean-architecture/REFERENCE.md) | **Dismissed after reading.** `CatalogBrowser` is presentation-only state with a `CatalogPageQuery` delegate seam; `CatalogBrowserTests` exercises it without a renderer. An interface would add a name and prove nothing. |

### L2 detail

`WorkspaceView` (an Application read model) carries `DraftState`, and `AssignableSlot` / `QuoteLine`
carry `SlotId` — both `CoreRentalNet.Modules.Workspace.Domain` types. The presentation reads the
domain enum directly to choose a glyph, a CSS class and a catalog query:

```
Host/Presentation/SlotCss.cs:17      SlotId.Desk => "slot--desk",
Host/Presentation/SlotCatalog.cs:27   SlotId.Desk => new SearchCatalogQuery(Category: CatalogCategory.Desk),
Host/Presentation/ProductGlyph.cs:28  SlotId.Desk => "desk",
```

That is allowed in direction (presentation depends inward), but the rule says the page's shape is
the page's own, produced by translation. This is the same root cause as modular finding M1; the two
audits see one issue from two sides.

## What is already correct

**Dependency direction (rule group A).** `Domain` references nothing but `BuildingBlocks.Domain`.
`Application` references `Domain` (+ `BuildingBlocks.Application`). `Infrastructure` references
`Domain` and EF Core. Only the composition root (`Host/Composition`, `Host/Program.cs`) references
an `Infrastructure` project. The checker agrees: 0 `OUTWARD_DEPENDENCY`, 0
`FRAMEWORK_TYPE_IN_INNER_LAYER`.

**Layer contents (rule group B).** `Domain` holds plain records (`Rental`, `Invoice`, `Workspace`,
`SlotAssignment`) and value types, no ORM attributes, no base class, no self-save. `Application`
holds the rule services and the ports. `Infrastructure` holds the EF Core adapters and the SQLite
kernel. `Host/Presentation` holds page-only shapes (`CatalogGroup`, `CatalogTab`, `SlotCatalog`).
Verified by grep: no `JsonPropertyName`, `DbContext`, `DbSet`, `[Key]` or `[Table]` anywhere above
`Infrastructure`.

**Ports and adapters (rule group C).** Every port sits inward and every implementation outward:
`IProductCatalog`, `IWorkspaceRepository`, `IRentalRepository`, `IInvoiceRepository`,
`IUnitOfWork`, `INumberSequence`, `IPlaceOrder`, `IConvertWorkspaceToOrder`. Callers inject the
interface; the `sealed` implementation is named only in the composition root.

**DTO placement (rule group D).** Use-case inputs/outputs sit beside their handler
(`AssignProductCommand`, `CheckoutResult`, `PlaceOrderResult`). Read models sit beside the query that
builds them (`Rentals.Application/Queries/Views`). Cross-module shapes sit in the owning module's
`Application/Contracts` (`IProductCatalog`, `ProductView`, `IConvertWorkspaceToOrder`). No
solution-wide `Dtos/` folder, and no `…Dto` type name.

**Mapping placement (rule group E).** `ProductViewMapper` (Infrastructure) builds the Application
contract; `WorkspaceViewService` and `WorkspaceQuoteService` (Application) build the read models
because they need a rule service; money formatting never sits in a mapper. No business arithmetic in
a repository: `MoneyConversion` and `SqliteNumberSequence` translate, they do not decide.

**Composition root (rule group F).** One folder, `Host/Composition`, one file per concern, one
method per concern. No service locator outside it (checker: 0 `SERVICE_LOCATOR`). No consumer
constructs its own dependency. Lifetimes are deliberate: the catalog is a singleton immutable
snapshot, the draft and the rentals contexts are scoped, `CatalogBrowser` is transient because each
surface owns its own browsing state, and `RenewalSchedulerHostedService` opens its own scope to
reach the scoped scheduler.

**Module boundaries (rule group G).** Covered by `ModuleBoundaryTests` ARC-01 and by the companion
modular-monolith audit (`specs/MODULE_AUDIT_LATEST.md`).

**Tests (rule group H).** Every inner layer is unit-testable with no database, browser or server:
`BuildingBlocks.UnitTests` (28), `Modules.Catalog.UnitTests` (50), `Modules.Rentals.UnitTests` (94),
`Modules.Workspace.UnitTests` (54). Adapters are integration-tested (`IntegrationTests`, 54) and the
UI end-to-end (`CoreRentalNet.E2E`). Hand-written fakes (`InMemoryWorkspaceRepository`,
`InMemoryRentalRepository`, `StubProductImage`, `RecordingUnitOfWork`) prove the ports are small
enough to fake; there is no mocking library.

## Enforced by the architecture tests

| Clean rule | Test |
|---|---|
| Inner layers do not name outer types | `ModuleBoundaryTests` ARC-01, `ApplicationDependencyTests` ARC-04 |
| No EF Core / ASP.NET / SQLite in Domain | `ModuleBoundaryTests` ARC-04 |
| No EF Core / ASP.NET / SQLite in Application | `ApplicationDependencyTests` ARC-04 **(new this round)** |
| One DbContext per module; module-prefixed tables | `SourceLayoutTests` ARC-02/03 |
| No direct clock read in Domain or Application | `SourceLayoutTests` ARC-06 |
| One type per file, named after the type | `OneTypePerFileTests` |
| ≤150 code lines per file, ≤40 per method, ≤3 nesting levels | `CodingStandardTests` |
| Catalog contracts public and complete | `ModuleBoundaryTests.The_catalog_application_contracts_are_public_and_complete` |

## Documented exceptions (confirmed, not findings)

| Exception | Evidence |
|---|---|
| A persisted record has public setters | `Workspace`, `Rental`, `Invoice` use settable properties; a rule service (`IWorkspaceService`, `IRentalLifecycleService`, `IInvoiceService`) owns the invariants. The behaviour lock pins the rules. |
| `Catalog/Infrastructure/Contracts` holds a published type | `ProductLoadException` is reachable only inside `Catalog.Infrastructure`; the checker's `Infrastructure.Contracts` suppression covers the convention. |
| The composition root references `Infrastructure` | `Host/Composition/*Registration.cs` and `Host/Program.cs` only. |
| A hosted service opens a scope | `RenewalSchedulerHostedService` — the only way a singleton may reach a scoped scheduler. |

## Round log

This file is the round-1 report, updated in place as rounds 2–5 fixed what it found. The loop is recorded in
`specs/REVIEW_ROUNDS.md`. Rounds 3–5 changed no layering: the two classes split later (`RentalScheduler` →
`RenewalInvoiceIssuer`; `CheckoutCommandHandler` → `CheckoutConfirmation`) put new types in their existing
layers and added no dependency edge. `check-layering.sh` stayed at 0 flags through every round.

## Not committed

Every change was a file edit, a build or a test. No `git add`, `git mv`, `git restore`, `git
checkout` or `git commit` was run.
