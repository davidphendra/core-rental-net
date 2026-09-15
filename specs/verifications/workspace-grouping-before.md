# Workspace grouping — Stage 0 baseline (BEFORE)

Recorded before any file move, per `specs/REFACTOR_WORKSPACE_GROUPING_LATEST.md` Stage 0.
The working tree already carried unrelated in-flight work; these numbers are the tree as it stood,
and are the hard gate every later stage must match.

Commands (from `.github/workflows/ci.yml`):

```
BUILD      = dotnet build CoreRentalNet.sln --configuration Release
NONBROWSER = dotnet test CoreRentalNet.sln --configuration Release --no-build --filter "FullyQualifiedName!~CoreRentalNet.E2E"
BROWSER    = dotnet test tests/CoreRentalNet.E2E --configuration Release --no-build
```

| Baseline | Result | Counts |
|---|---|---|
| BUILD | succeeded | 0 warnings, 0 errors |
| NONBROWSER | green | 481 passed, 0 failed, 0 skipped (8 projects) |
| BROWSER | green | 103 passed, 1 skipped, 0 failed (104 total) |

NONBROWSER per project:

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| CoreRentalNet.BuildingBlocks.UnitTests | 28 | 0 | 0 |
| CoreRentalNet.BehaviourLock | 37 | 0 | 0 |
| CoreRentalNet.Modules.Workspace.UnitTests | 54 | 0 | 0 |
| CoreRentalNet.Modules.Rentals.UnitTests | 94 | 0 | 0 |
| CoreRentalNet.Modules.Catalog.UnitTests | 50 | 0 | 0 |
| CoreRentalNet.ArchitectureTests | 49 | 0 | 0 |
| CoreRentalNet.Host.Tests | 115 | 0 | 0 |
| CoreRentalNet.IntegrationTests | 54 | 0 | 0 |
| **Total** | **481** | **0** | **0** |

BROWSER: `CoreRentalNet.E2E.Flows.RealTenantTests.The_tenant_signs_the_account_in_and_the_gate_answers`
is the single skip (external-tenant test, `RealTenantFactAttribute`).

SDK: 10.0.400 (`global.json`, `rollForward: latestPatch`).
