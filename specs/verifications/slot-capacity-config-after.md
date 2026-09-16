# Workspace slot capacities through appsettings — verification

Moves the per-slot capacity thresholds out of `SlotRuleProvider`'s hardcoded table and into
configuration (`Workspace:SlotCapacity:*`). Defaults are unchanged, so behaviour is unchanged until
an environment overrides a value.

Commands (as in `workspace-grouping-after.md`):

```
BUILD      = dotnet build CoreRentalNet.sln --configuration Release
NONBROWSER = dotnet test CoreRentalNet.sln --filter "FullyQualifiedName!~CoreRentalNet.E2E"
BROWSER    = dotnet test tests/CoreRentalNet.E2E
```

| Baseline | Before | After | Result |
|---|---|---|---|
| BUILD | 0 warnings, 0 errors | 0 warnings, 0 errors | unchanged |
| NONBROWSER | 482 passed, 0 failed | 488 passed, 0 failed | +6 new tests (5 unit, 1 architecture) |
| BROWSER | 103 passed, 1 skipped | 103 passed, 1 skipped | unchanged |

The +6 are the tests that prove the change: the shipped defaults still reproduce the pinned table,
an override (`Monitor: 5`) reaches `ISlotRuleProvider` and the refusal message, a capacity below one
is refused, and the committed `appsettings.json` matches the code defaults. `WorkspaceRuleLockTests`
and `SlotRulesTests` were not edited, which is the proof the requirement did not change.

## What changed

- `WorkspaceSlotSettings` (new, `Workspace.Application/Rules/`) — the configurable
  capacities, defaulting to the shipped table.
- `SlotRuleProvider` — reads `MaxQuantity` from the settings; keeps display names and the mandatory
  flags; parameterless constructor preserved for the existing tests and the behaviour lock.
- `WorkspaceRegistration.ReadSlotSettings` — reads `Workspace:SlotCapacity:<Slot>`, falling back to
  the shipped default per key.
- `appsettings.json` — the committed `Workspace:SlotCapacity` section.
