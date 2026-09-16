# Refactor plan — group the Workspace module under Contracts, Queries and Workspace

Status: PROPOSED — not started. No code has been changed.

## Problem Statement

`CoreRentalNet.Modules.Workspace.Application` has five top-level folders that do not agree on what
they mean:

| Folder | What is actually in it | Problem |
|---|---|---|
| `Contracts/` | published interfaces **and** their concrete implementations | the published surface and its behaviour are in one bucket |
| `Queries/` | query records, query ports, query handlers — but **no read models** | the records the queries return sit in `Workspace/` |
| `Workspace/` | 17 files, flat: 5 ports, 5 services, 5 read models, 2 helpers | a flat bucket; nothing groups by job |
| `Commands/<Operation>/` | command record, command port, handler | a fourth naming style (`<Operation>/`), outside the three buckets |
| `Catalog/` | one static mapper (`CatalogSlotMapping`) | a fifth folder for one file |

The same three jobs are spread across four folders, and two folders hold classes that belong to
another folder. A reader cannot answer "where do read models live?" or "where does a command go?"
from the tree. The sibling modules already answer this — `Rentals.Application` uses `Queries/`,
`Views/`, and feature folders; `Catalog.Application` uses `Contracts/` plus one folder per query —
so the module is also inconsistent with the repository.

## Solution

Reduce the Application project to the three buckets the product owner named —
`Workspace/Contracts`, `Workspace/Queries`, `Workspace/Workspace` — and place every existing class
in the bucket that matches its job:

- **Contracts** — the published, cross-module surface (Rentals and the composition root call it).
- **Queries** — everything on the read path: query messages, query ports, query handlers, query
  services and the view/read-model records they return.
- **Workspace** — the write path and the rules: command messages, command ports, command handlers,
  the write service, the slot rules and the internal helpers.

Each bucket gains sub-folders so that a class's job is readable from its path. Nothing is renamed,
nothing changes behaviour, and no public type is deleted or invented.

**Invariant the whole refactor must preserve (hard gate):** type names, public signatures and
behaviour are unchanged; the three baselines (`BUILD`, `NONBROWSER`, `BROWSER`) stay green at the
start and the end of every stage. This is a move-and-update-usings refactor only.

## Target layout

Recommended (Option A, the one this plan executes):

```
CoreRentalNet.Modules.Workspace.Application/
├─ Contracts/                      published cross-module surface
│   ├─ Composition/                IDefineWorkspaceComposition, DefineWorkspaceComposition,
│   │                              WorkspaceComposition, WorkspaceCompositionLine
│   └─ Conversion/                 IConvertWorkspaceToOrder, ConvertWorkspaceToOrder,
│                                  WorkspaceConversion
├─ Queries/                        read path
│   ├─ GetWorkspace/               GetWorkspaceQuery, IGetWorkspaceHandler, GetWorkspaceHandler
│   ├─ GetWorkspaceQuote/          GetWorkspaceQuoteQuery, IGetWorkspaceQuoteHandler, GetWorkspaceQuoteHandler
│   ├─ Services/                   IWorkspaceQueryService, WorkspaceQueryService,
│   │                              IWorkspaceQuoteService, WorkspaceQuoteService,
│   │                              IWorkspaceViewService, WorkspaceViewService
│   └─ Views/                      WorkspaceView, WorkspaceQuote, QuoteLine,
│                                  AssignableSlot, AssignableItem
└─ Workspace/                      write path and rules
    ├─ Commands/<Operation>/       5 × { <Operation>Command, I<Operation>Handler, <Operation>Handler }
    ├─ Services/                   IWorkspaceService, WorkspaceService
    ├─ Rules/                      ISlotRuleProvider, SlotRuleProvider, CatalogSlotMapping
    └─ Support/                    DeliveryAddressNormalizer, WorkspaceResolver
```

The whole move, class by class:

| Class (current folder) | Target folder | Bucket reason |
|---|---|---|
| `IConvertWorkspaceToOrder`, `ConvertWorkspaceToOrder`, `WorkspaceConversion` | `Contracts/Conversion/` | cross-module conversion contract |
| `IDefineWorkspaceComposition`, `DefineWorkspaceComposition`, `WorkspaceComposition`, `WorkspaceCompositionLine` | `Contracts/Composition/` | cross-module read-of-a-draft contract |
| `GetWorkspaceQuery`, `IGetWorkspaceHandler`, `GetWorkspaceHandler` | `Queries/GetWorkspace/` | read path |
| `GetWorkspaceQuoteQuery`, `IGetWorkspaceQuoteHandler`, `GetWorkspaceQuoteHandler` | `Queries/GetWorkspaceQuote/` | read path |
| `IWorkspaceQueryService`, `WorkspaceQueryService` | `Queries/Services/` | read path |
| `IWorkspaceQuoteService`, `WorkspaceQuoteService` | `Queries/Services/` | read path |
| `IWorkspaceViewService`, `WorkspaceViewService` | `Queries/Services/` | read path |
| `WorkspaceView` | `Queries/Views/` | read model |
| `WorkspaceQuote`, `QuoteLine` | `Queries/Views/` | read model |
| `AssignableSlot`, `AssignableItem` | `Queries/Views/` | read model |
| `AssignProductCommand` + `IAssignProductHandler` + `AssignProductHandler` | `Workspace/Commands/AssignProduct/` | write path |
| `ChangeQuantityCommand` + `IChangeQuantityHandler` + `ChangeQuantityHandler` | `Workspace/Commands/ChangeQuantity/` | write path |
| `RemoveAssignmentCommand` + `IRemoveAssignmentHandler` + `RemoveAssignmentHandler` | `Workspace/Commands/RemoveAssignment/` | write path |
| `SetDeliveryAddressCommand` + `ISetDeliveryAddressHandler` + `SetDeliveryAddressHandler` | `Workspace/Commands/SetDeliveryAddress/` | write path |
| `StartDraftCommand` + `IStartDraftHandler` + `StartDraftHandler` | `Workspace/Commands/StartDraft/` | write path |
| `IWorkspaceService`, `WorkspaceService` | `Workspace/Services/` | write path |
| `ISlotRuleProvider`, `SlotRuleProvider` | `Workspace/Rules/` | slot table (a write rule) |
| `CatalogSlotMapping` (was `Catalog/`) | `Workspace/Rules/` | maps a catalog category to a slot id |
| `DeliveryAddressNormalizer`, `WorkspaceResolver` | `Workspace/Support/` | internal helpers |
| `DraftToken`, `SlotId`, `SlotRule`, `Workspace`, `…` (Domain project) | **unchanged** | out of scope |

`Catalog/` disappears: its one file moves to `Workspace/Rules/` and the folder is removed.

## Group name candidates

This is the part the product owner asked to see. The three top-level names are fixed by the request;
the sub-groups are the open choice.

### Top-level buckets (fixed, alternatives listed for the record)

| Bucket | Recommended | Alternatives considered | Why the recommendation wins |
|---|---|---|---|
| Published surface | `Contracts` | `Ports`, `Api`, `Abstractions`, `PublishedApi` | already the name in `Catalog.Application`; the types cross a module boundary |
| Read side | `Queries` | `Reads`, `ReadModels`, `Querying`, `Projections` | already the name in `Rentals.Application`; holds more than models |
| Write side | `Workspace` | `Core`, `Model`, `Behaviour`, `WriteModel` | keeps the module's own name on its own behaviour, and no type is named `Core` today |

### Sub-group candidates

| Parent | Recommended | Alternatives considered | Why |
|---|---|---|---|
| `Contracts/` | `Composition/`, `Conversion/` | `Inbound/`+`Outbound/`; `Draft/`+`Order/`; flat | the two published jobs are named after the interface verbs (`DefineComposition`, `Convert`) |
| `Queries/` | per-query folder + `Services/` + `Views/` | flat `Queries/` (today); `Messages/`+`Handlers/`+`ReadModels/` | matches `Rentals.Application/Views` and `Catalog.Application/<Operation>/` |
| `Queries/Views/` | `Views` | `ReadModels`, `Dtos`, `Projections` | `Rentals.Application` already ships `Views/`; the type names already end in `View`/`Quote` |
| `Queries/Services/` | `Services` | `Handlers`, `Querying`, flat | the classes are `…Service`, and the query *handlers* are in their own per-query folders |
| `Workspace/` | `Commands/`, `Services/`, `Rules/`, `Support/` | `Operations/`+`Policies/`+`Internal/`; flat (today) | `Commands/` already exists; `Rules` names the slot table and the mapper together |
| `Workspace/Rules/` | `Rules` | `Slots`, `Policies`, `SlotRules` | one name covers both `ISlotRuleProvider` and `CatalogSlotMapping` |
| `Workspace/Support/` | `Support` | `Internal`, `Helpers`, `Infrastructure` | the two types are `internal static`; `Infrastructure` is a reserved layer word here |

Decision to confirm before the first commit: **rename namespaces with the folders, or move folders
only?** This plan assumes *rename namespaces to match the folders* (the `RootNamespace` convention
the project already sets, and what `Catalog`/`Rentals` do). If the answer is "folders only", the
usings do not change and C1–C7 below collapse into one commit.

## Commits

Shorthand for the baselines, taken from `.github/workflows/ci.yml`:

```
BUILD      = dotnet build CoreRentalNet.sln --configuration Release
NONBROWSER = dotnet test CoreRentalNet.sln --configuration Release --no-build --filter "FullyQualifiedName!~CoreRentalNet.E2E"
BROWSER    = dotnet test tests/CoreRentalNet.E2E --configuration Release --no-build
```

**Stage 0 — lock the behaviour (mandatory, before any move)**

0. Record `BUILD`, `NONBROWSER`, `BROWSER` counts in `specs/verifications/` (a new
   `workspace-grouping-before.md`) → verify: all three green, numbers written down.

**Stage 1 — the read side (`Queries/`)**

1. Create `Queries/GetWorkspace/` and move `GetWorkspaceQuery.cs`, `IGetWorkspaceHandler.cs`,
   `GetWorkspaceHandler.cs` into it; rewrite the three `namespace` lines and every `using` that
   names them → verify: `BUILD`.
2. Same for `Queries/GetWorkspaceQuote/` (`GetWorkspaceQuoteQuery.cs`, `IGetWorkspaceQuoteHandler.cs`,
   `GetWorkspaceQuoteHandler.cs`) → verify: `BUILD`.
3. Create `Queries/Views/`, move `WorkspaceView.cs`, `WorkspaceQuote.cs`, `QuoteLine.cs`,
   `AssignableSlot.cs`, `AssignableItem.cs`; update namespaces and usings → verify: `BUILD`.
4. Create `Queries/Services/`, move the six `IWorkspaceQueryService`/`WorkspaceQueryService`/
   `IWorkspaceQuoteService`/`WorkspaceQuoteService`/`IWorkspaceViewService`/`WorkspaceViewService`
   files; update namespaces and usings → verify: `BUILD`.
5. Stage-1 close: run all three baselines and record the counts → verify: `BUILD`, `NONBROWSER`,
   `BROWSER` green, equal to the Stage-0 numbers.

**Stage 2 — the published surface (`Contracts/`)**

6. Create `Contracts/Composition/`, move `IDefineWorkspaceComposition.cs`,
   `DefineWorkspaceComposition.cs`, `WorkspaceComposition.cs`, `WorkspaceCompositionLine.cs`;
   update namespaces and usings → verify: `BUILD`.
7. Create `Contracts/Conversion/`, move `IConvertWorkspaceToOrder.cs`,
   `ConvertWorkspaceToOrder.cs`, `WorkspaceConversion.cs`; update namespaces and usings →
   verify: `BUILD`.
8. Stage-2 close: run all three baselines → verify: green, unchanged.

**Stage 3 — the write side (`Workspace/`)**

9. Move the five `Commands/<Operation>/` folders under `Workspace/Commands/` in one commit (the
   internal shape of each folder is already right); update namespaces and usings → verify: `BUILD`.
10. Create `Workspace/Services/`, move `IWorkspaceService.cs`, `WorkspaceService.cs`; update →
    verify: `BUILD`.
11. Create `Workspace/Rules/`, move `ISlotRuleProvider.cs`, `SlotRuleProvider.cs`,
    `CatalogSlotMapping.cs` (from `Catalog/`), delete the now-empty `Catalog/`; update → verify:
    `BUILD`.
12. Create `Workspace/Support/`, move `DeliveryAddressNormalizer.cs`, `WorkspaceResolver.cs`;
    update → verify: `BUILD`.
13. Stage-3 close: run all three baselines → verify: green, unchanged.
14. Update `CLAUDE.md` if it names any moved path; grep `specs/` for the old folder names and fix
    the references that are still authoritative. Do **not** touch the files staged for deletion
    (CLN-10) → verify: `BUILD`.

Every commit leaves the tree compiling and is individually revertible with `git revert`.

## Decision Document

- **Scope:** `src/Modules/Workspace/CoreRentalNet.Modules.Workspace.Application` only. The Domain
  project and the Infrastructure project already have their own shapes and are not touched.
- **No behaviour change.** No method body, signature, registration or DI lifetime changes. If a
  compile error is not a `using`/`namespace` fix, the step is wrong and must be reverted.
- **Namespace follows folder.** New namespaces are
  `CoreRentalNet.Modules.Workspace.Application.{Contracts.Composition | Contracts.Conversion |
  Queries.GetWorkspace | Queries.GetWorkspaceQuote | Queries.Views | Queries.Services |
  Workspace.Commands.<Operation> | Workspace.Services | Workspace.Rules | Workspace.Support}`.
- **`Workspace/Queries` and `Domain.Workspace`.** Files inside a namespace whose last segment is
  `Workspace` already qualify the domain record as `Domain.Workspace`; the command handlers keep
  that qualification. No new ambiguity is introduced, but the build verifies it.
- **Registrations change only their usings.** `Host/Composition/WorkspaceRegistration.cs` binds
  interfaces to implementations; the type names do not move, so the registration lines are
  unchanged. Only its `using` list is edited.
- **`IWorkspaceQueryService` and friends live in `Queries/Services/`,** not in `Workspace/Services/`,
  because their only job is to read. `WorkspaceService` stays in `Workspace/Services/` because it is
  the only writer.
- **`CatalogSlotMapping` moves to `Workspace/Rules/`.** It is a pure static mapper over the slot
  table; a `Catalog/` folder holding one file is the smell the plan removes.
- **`Views/` keeps its name** rather than becoming `ReadModels/`, to match `Rentals.Application`.
- **Not a Clean Architecture change.** Type placement stays inside the Application layer; the
  dependency direction (ARC-01/ARC-04) is untouched.

## Testing Decisions

- **No new tests.** This refactor changes no behaviour, so a new test would test the filesystem, not
  the product. The gate is the existing suite plus the compiler.
- **Existing coverage is the safety net.** `tests/CoreRentalNet.BehaviourLock` holds the behaviour
  lock; `tests/CoreRentalNet.Modules.Workspace.UnitTests` (commands, quote, composition, slot
  rules, start draft) covers the Application classes; `tests/CoreRentalNet.IntegrationTests` covers
  persistence and end-to-end checkout; `tests/CoreRentalNet.E2E` is the browser baseline.
- **Architecture tests already hold the invariants that matter here.** `OneTypePerFileTests` keeps
  one type per file (moving a file does not change this); `CodingStandardTests` keeps the line and
  method limits; `ModuleBoundaryTests` keeps the dependency direction. `SourceLayoutTests` reads
  source by module, not by folder, so it is unaffected.
- **A good test here says what the reader sees:** the path is the job. That is verified by review of
  the final tree, not by a test — and the compiler guarantees the move is complete.
- **Non-vacuity of the baseline:** the Stage-0 counts must be non-zero and equal to the Stage-1/2/3
  counts. If a baseline count drops, a test was skipped, not passed.

## Out of Scope

- No rename of any type, method or property.
- No move of `Workspace.Domain` or `Workspace.Infrastructure` files.
- No split of the large types (`WorkspaceService` is 197 lines, already at the `CodingStandardTests`
  ceiling); that is a behaviour-shaped refactor and needs its own plan.
- No change to `Host`, `Rentals` or `Catalog` beyond the `using` lines caused by this move.
- The 56 spec files staged for deletion (CLN-10) are not restored, edited or referenced.
- No `git add`, `git commit` or branch operation is implied by this plan; the follow-up is
  `kickoff-branch` when the product owner approves.

## Further Notes

- The plan mirrors the two module layouts that already work: `Rentals.Application` (`Queries/`,
  `Views/`, feature folders) and `Catalog.Application` (`Contracts/`, one folder per query). If the
  three-bucket split is kept, consider applying the same shape to `Rentals.Application` later — its
  `Checkout/` and `Orders/` folders are the write side it currently has no name for.
- The namespace-rename blast radius is about 120 `using` lines, mostly in tests
  (`CoreRentalNet.Modules.Workspace.Application.Workspace` alone appears 28 times). That is why the
  commits are split by bucket: a failed step reverts to a namespace that still compiles, not to a
  half-moved tree.
