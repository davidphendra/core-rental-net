# Baselines

A step is done only when all five baselines are green. The numbers are recorded where the step is
described, and the work is not done until they are.

| Name | Command | Where measured | Green means |
|---|---|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln -c Release` | repository root | exit 0, no warnings (warnings are errors) |
| `NONBROWSER` | `dotnet test CoreRentalNet.sln -c Release --no-build --filter "FullyQualifiedName!~CoreRentalNet.E2E"` | repository root | 0 failed |
| `TOOL` | `dotnet test src/Tools/CatalogIngestion.sln -c Release` | repository root | 0 failed |
| `BROWSER` | `pwsh tests/CoreRentalNet.E2E/bin/Release/net10.0/playwright.ps1 install chromium`, then `dotnet test tests/CoreRentalNet.E2E -c Release --no-build` | repository root | 0 failed |
| `AGENT` | `dotnet test agentfoundry/AgentFoundry.sln -c Release` | `agentfoundry/` | 0 failed |

The browser suite drives a real Chromium over a real socket, so the browser has to be installed
first; the install script ships with the Playwright package, so its revision always matches the
library. The `AGENT` baseline is the agent tree's own solution, which is separate from the
application's and imports the repository root's `Directory.Build.props` — a change there is a change
to the agent build too. `TOOL` is the ingestion tool's own solution, split out of the application's
so that an operator's one-shot run is not part of the application's build; no other baseline compiles
it and neither pipeline builds it, so it is a local check in the same sense `BROWSER` is.

Two checks are not baselines because they need a deployment, and deployment needs the product
owner's explicit consent:

| Name | Command | Green means |
|---|---|---|
| `SMOKE` | the deployed host's footer shows the version | the footer matches the tag that was built |
| `AGENT-DEPLOY` | `azd ai agent invoke …` against the deployment | the `[startup]` version line matches the tag that was built, and the `git sha`, `build id` and `environment` lines match the run and the environment stamped into the artifact |

## Last measured

Measured on 2026-10-07, on `main`, after the build identity reached the artifact and the business
facts reached Application Insights:

| Baseline | Result |
|---|---|
| `BUILD` | 0 warnings, 0 errors |
| `NONBROWSER` | 797 passed, 0 failed, 0 skipped |
| `TOOL` | 42 passed, 0 failed, 0 skipped |
| `AGENT` | 184 passed, 0 failed |
| `BROWSER` | **not run** — the `CoreRentalNet.E2E.LocalAgent` stand-in was deleted in `a6e127e` while `HostFixture` still starts it. Pre-existing and unrelated to this change; the browser tier cannot start until that fixture is restored. |

`NONBROWSER` rose from 781 by sixteen: six facts for the build identity and its reader, five for the
business meter, one configuration guard that keeps the telemetry connection string out of every committed
file, two for the start-up announcement, and two for the Foundry credential choice committed immediately
before all of it. `NONBROWSER` no longer counts the ingestion tool's 42 tests, which the `TOOL` row above
now records.
