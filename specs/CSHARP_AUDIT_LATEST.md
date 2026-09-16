# C# audit

Skill: `audit-csharp`, rule catalog
[`REFERENCE.md`](../../.pi/agent/skills/audit-csharp/REFERENCE.md). Date: 2026-09-16.
Scope: the whole solution (`src/`); tests read only for the flags the mechanical check reports.

## Verdict

| Check | Command | Result |
|---|---|---|
| Mechanical check, `src` | `check-csharp.sh src` | 4 `CATCH_ALL` hits (all read and dismissed) |
| Mechanical check, `tests` | `check-csharp.sh tests` | 12 `WALL_CLOCK` hits (all read and dismissed) |
| Checker self-test | `check-csharp.sh --self-test` | self-test OK |
| Build | `dotnet build CoreRentalNet.sln` | succeeded, **0 warnings, 0 errors** |
| Nullable / warnings-as-errors / analyzers / `.editorconfig` | `Directory.Build.props` | `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<AnalysisLevel>latest</AnalysisLevel>`, `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` |

## Findings

| ID | Severity | File:line | Rule | Source | Fix |
|---|---|---|---|---|---|
| CS1 | MEDIUM | `Host/Presentation/CatalogBrowser.cs:23` (`ask`); `Catalog/Infrastructure/ProductCatalog.cs:18` (`views`); `Host/Presentation/AccessoryGroups.cs:19` (`Order`); `Workspace/Application/Rules/SlotRuleProvider.cs:18` (`Rules`) | `_camelCase` for private instance fields; `s_` for private static fields | [NAMES](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/identifier-names) | **Fixed.** Renamed to `_ask`, `_views`, `s_order`, `s_rules`. |
| CS2 | MEDIUM | 5 command handlers + `Host/Presentation/WorkspaceSession.cs` | Write XML docs on public members | [CONV](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions) | **Fixed in scope**, listed repo-wide. Every public member of a type that implements a documented interface now carries `<inheritdoc />`. A repo-wide gap remains — see below. |
| CS3 | **HIGH (open, one class)** | `Rentals/Application/Commands/PlaceOrder/PlaceOrderService.cs:21` (10 dependencies). `RentalScheduler` (9 → **5**) and `CheckoutCommandHandler` (7 → **5**) were split this loop. | A class with many injected dependencies probably has many responsibilities. Split it. | [DI](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection-guidelines), [ARCH](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles) | **Partially fixed; `PlaceOrderService` open — user decision requested.** See below. |
| CS4 | LOW | `PermissionClaims.cs:63`, `WorkspaceSession.cs:109`, `RentalScheduler.cs:47`, `RenewalSchedulerHostedService.cs:74` | Do not catch `Exception` without a filter or a real handler | [EX](https://learn.microsoft.com/dotnet/standard/design-guidelines/exception-throwing) | **Dismissed after reading each line.** Three carry a `when` filter (`SecurityTokenException or ArgumentException`; `DomainRuleViolationException or NotFoundException`; `exception is not OperationCanceledException`). The fourth is a background loop whose real handler logs and continues, so one bad order does not stop the book. |
| CS5 | LOW | `SourceLayoutTests.cs:81–84`, `E2E/ProcessPool.cs:67–124`, `E2E.LocalProvider/Program.cs:135–260` | Do not read the wall clock in application code | [CONV](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions) | **Dismissed.** These are test sources: the architecture test asserts the rule, and the E2E process pool builds absolute timeouts. `src/` has zero clock reads outside injected `TimeProvider` (enforced by `SourceLayoutTests` ARC-06). |

## CS3 — the wide constructors, read rather than counted

Two of the three were split during the five-round loop; the third is open.

| Class | Before | After | What the class actually does | Disposition |
|---|---|---|---|---|
| `RentalScheduler` | 9 | **5** | One public operation: advance every schedulable rental one pass. The billing half moved to `RenewalInvoiceIssuer` (6 dependencies), which owns one thing: issue and settle every started, unbilled period. | **Fixed.** Both classes are single-purpose and smaller than before. |
| `CheckoutCommandHandler` | 7 | **5** | One public operation: turn a draft into an order. Rebuilding the confirmation of an order that already exists moved to `CheckoutConfirmation` (3 dependencies). | **Fixed.** |
| `PlaceOrderService` | 10 | 10 | One public operation: place one order. The 10 dependencies are the collaborators one transaction needs: two repositories, a token service, an invoice service, a lifecycle service, a delivery policy, a number sequence, a unit of work, settings and a clock. | **Open.** See below. |

`PlaceOrderService` stays at 10. Reading it shows a single responsibility — one order, one transaction. The 10
collaborators are what the transaction touches, not two jobs glued together. Extracting an `OrderAssembler`
would move five of them into a new type and leave seven, so the maximum width falls by three at the cost of
a name the domain does not have. The repository's own authority chain (`CLAUDE.md`) states *small classes,
KISS, no speculative abstraction*.

**Disposition: confirmed smell, recorded OPEN, not split.** The skill's rule is a heuristic ("probably has many
responsibilities"); reading the class shows one. The user should decide between (a) accepting the transaction
script with this note, or (b) authorising the `OrderAssembler` extraction as its own story, with the behaviour
lock as the safety net. This audit does not declare the HIGH closed.

## CS2 — the documentation gap, counted

49 public declarations in `src/` have no `///` on the member above them. Filtered to members of
**public** types (a member of an `internal` composition-root class is not public API), the meaningful
remainder clusters in four groups:

| Group | Examples | Note |
|---|---|---|
| Interface implementations of the files changed this loop | `WorkspaceSession`, the five workspace command handlers | **Fixed** with `<inheritdoc />`. |
| Number value types | `RentalNumber.Of/Parse/TryParse`, `InvoiceNumber.Of/Parse/TryParse`, `RentalId.New/From`, `InvoiceId.New/From`, `WorkspaceId.New/From` | **Fixed.** `Parse` and `From` now name the `DomainRuleViolationException` they throw (rule F MEDIUM). |
| Public static helpers | `ProductGlyph.ForProduct/ForSlot/PathFor`, `SlotCss.ClassFor`, `SlotCatalog.ForSlot`, `LocalUrl.Sanitise/IsLocal`, `SignInUrl.For` | Open. Each is small and named for its job; a summary would restate the name. |
| Persistence records | `Rental`, `Invoice`, `Workspace`, `SlotAssignment` properties | Open. The type-level summary covers the record; the properties are ORM surface. |

A second round should close the public static helpers first, because they are the only remaining group that is
public API rather than ORM surface.

## Round log

This file is the round-1 report, updated in place as rounds 2–5 fixed what it found. The loop is recorded in
`specs/REVIEW_ROUNDS.md`. Final state: CS1 fixed, CS2 fixed for contract members and value types, CS3 fixed for
two of three classes and open for `PlaceOrderService`, CS4 and CS5 dismissed after reading.

## What is already correct

**Naming (rule group A).** PascalCase types, methods, properties and constants; camelCase
parameters and locals; `I`-prefixed interfaces; every asynchronous method ends in `Async`
(`HandleAsync`, `RunOnceAsync`, `ReserveNextAsync`, `IssueFor`, `Settle` are synchronous and say so);
names state meaning, not type.

**Organization (rule group B).** File-scoped namespaces everywhere; every namespace matches its
folder; one type per file (`OneTypePerFileTests`); `using` directives outside the namespace with
`System.*` first; no `Common`/`Utils`/`Helpers` namespace.

**Responsibility (rule group C).** One public operation per service is the repository's stated rule
and it holds. File and method size are enforced by `CodingStandardTests` and pass. The only open item
is the dependency count above.

**Design (rule group D).** Dependencies point at abstractions everywhere except the composition root.
No consumer instantiates its own dependency (`grep` for `new …Handler(` / `new …Repository(` in
`src/` returns nothing). No mutable static state: the four static fields are `const` or immutable
(`ImmutableArray`, `FrozenDictionary`).

**Language and data (rule group E).** Nullable enabled; no `!` used to silence a warning without a
reason (the two `null!` on EF-owned navigation properties are explained by the nullable annotations
on the property). `var` only where the right side makes the type obvious. Disposal uses `using` /
`await using`.

**Exceptions (rule group F).** Failures are thrown (`DomainRuleViolationException`, `NotFoundException`,
`ProductLoadException`), never returned as codes; `TryParse` is used where absence is normal;
specific types with meaningful messages; thrown exceptions are documented on the public contracts
(rule A3 of the previous building-blocks round).

**Async (rule group G).** No `.Result`, `.Wait()`, `async void` or `Thread.Sleep` in `src/`; every
`async` method awaits; `ConfigureAwait(false)` in library code; the one `ConfigureAwait(true)` flag is
absent.

**Tooling (rule group J).** `.editorconfig`, analyzers at `AnalysisLevel latest`, warnings as errors,
`EnforceCodeStyleInBuild` — all on. CI runs the non-browser tests with a filter that excludes E2E,
then the browser suite (the workflow is dormant until a remote exists).

## Not committed

Every change was a file edit, a build or a test. No `git add`, `git mv`, `git restore`, `git
checkout` or `git commit` was run.
