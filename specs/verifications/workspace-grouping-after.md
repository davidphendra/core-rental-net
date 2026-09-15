# Workspace grouping — AFTER

Recorded after all three buckets (`Contracts/`, `Queries/`, `Workspace/`) were completed.
Compare with `workspace-grouping-before.md`: every count must be equal and non-zero.

Commands (from `.github/workflows/ci.yml`):

```
BUILD      = dotnet build CoreRentalNet.sln --configuration Release
NONBROWSER = dotnet test CoreRentalNet.sln --configuration Release --no-build --filter "FullyQualifiedName!~CoreRentalNet.E2E"
BROWSER    = dotnet test tests/CoreRentalNet.E2E --configuration Release --no-build
```

| Baseline | Before | After | Result |
|---|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors | unchanged |
| NONBROWSER | 481 passed, 0 failed, 0 skipped | 481 passed, 0 failed, 0 skipped | unchanged |
| BROWSER | 103 passed, 1 skipped, 0 failed | 103 passed, 1 skipped, 0 failed | unchanged |

Per-bucket BUILD gate: green after Stage 1 (`Queries/`), Stage 2 (`Contracts/`), Stage 3 (`Workspace/`).
Per-bucket NONBROWSER (481) and BROWSER (103/1) gate: green at each close.

## Final layout

```
CoreRentalNet.Modules.Workspace.Application/
├─ Contracts/
│   ├─ Composition/   IDefineWorkspaceComposition, DefineWorkspaceComposition,
│   │                 WorkspaceComposition, WorkspaceCompositionLine
│   └─ Conversion/    IConvertWorkspaceToOrder, ConvertWorkspaceToOrder, WorkspaceConversion
├─ Queries/
│   ├─ GetWorkspace/       GetWorkspace, IGetWorkspace, GetWorkspaceHandler
│   ├─ GetWorkspaceQuote/  GetWorkspaceQuote, IGetWorkspaceQuote, GetWorkspaceQuoteHandler
│   ├─ Services/           IWorkspaceQueryService, WorkspaceQueryService,
│   │                      IWorkspaceQuoteService, WorkspaceQuoteService,
│   │                      IWorkspaceViewService, WorkspaceViewService
│   └─ Views/              WorkspaceView, WorkspaceQuote, QuoteLine,
│                          AssignableSlot, AssignableItem
└─ Workspace/
    ├─ Commands/<Operation>/  5 × { message, I…, Handler }
    ├─ Services/              IWorkspaceService, WorkspaceService
    ├─ Rules/                 ISlotRuleProvider, SlotRuleProvider, CatalogSlotMapping
    └─ Support/               DeliveryAddressNormalizer, WorkspaceResolver
```

`Commands/` and `Catalog/` no longer exist as top-level folders; `CatalogSlotMapping` is in
`Workspace/Rules/`. Every namespace now matches its folder.

## Scope of the change

- Move-and-update-usings only. No type, method or property renamed; no signature or behaviour
  changed; no public type added or deleted.
- `Workspace.Domain` and `Workspace.Infrastructure` were not touched.
- The 56 `specs/` files staged for deletion (CLN-10) were not touched.
