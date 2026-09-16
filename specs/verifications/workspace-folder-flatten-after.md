# Workspace folder flattening — AFTER

The `Workspace/` grouping inside `CoreRentalNet.Modules.Workspace.Application` was removed. Its four
children now sit directly under the Application folder, and the namespace segment
`…Application.Workspace.` became `…Application.`.

This is a pure move: no type, member or signature changed, and no behaviour changed. A move must leave
every count equal, and it does; compare with the counts in `workspace-grouping-after.md`.

| Baseline | Before | After | Result |
|---|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors | unchanged |
| NONBROWSER | 492 passed, 0 failed | 492 passed, 0 failed | unchanged |
| BROWSER | 103 passed, 1 skipped | 103 passed, 1 skipped | unchanged |

## Final layout

```
CoreRentalNet.Modules.Workspace.Application/
├─ Commands/<Operation>/   5 × { message, I…, Handler }
├─ Contracts/
│   ├─ Composition/   IDefineWorkspaceComposition, DefineWorkspaceComposition,
│   │                 WorkspaceComposition, WorkspaceCompositionLine
│   └─ Conversion/    IConvertWorkspaceToOrder, ConvertWorkspaceToOrder, WorkspaceConversion
├─ Queries/
│   ├─ GetWorkspace/       GetWorkspace, IGetWorkspace, GetWorkspaceHandler
│   ├─ GetWorkspaceQuote/  GetWorkspaceQuote, IGetWorkspaceQuote, GetWorkspaceQuoteHandler
│   └─ Views/              WorkspaceView, WorkspaceQuote, QuoteLine,
│                          AssignableSlot, AssignableItem
├─ Rules/                  ISlotRuleProvider, SlotRuleProvider, WorkspaceSlotSettings,
│                          CatalogSlotMapping
├─ Services/               IWorkspaceService, WorkspaceService,
│                          IWorkspaceQueryService, WorkspaceQueryService,
│                          IWorkspaceQuoteService, WorkspaceQuoteService,
│                          IWorkspaceViewService, WorkspaceViewService
└─ Support/                DeliveryAddressNormalizer, WorkspaceResolver
```

The layout above is the tree this step produced. The projection services were afterwards moved
from `Queries/Services/` to the top-level `Services/`; see `application-grouping-after.md` for the
current tree.

## What changed

- 23 files moved with `git mv`, so history follows.
- 90 references in 48 files (47 `.cs`, 1 `_Imports.razor`) updated from
  `CoreRentalNet.Modules.Workspace.Application.Workspace.` to
  `CoreRentalNet.Modules.Workspace.Application.`.
- The bare `…Application.Workspace` namespace held no type, only the four children, so the
  segment could be removed without a rename. `…Workspace.Domain.Workspace` was never matched.
