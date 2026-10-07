# Baselines

A step is done only when all four baselines are green. The numbers are recorded where the step is
described, and the work is not done until they are.

| Name | Command | Where measured | Green means |
|---|---|---|---|
| `BUILD` | `dotnet build CoreRentalNet.sln -c Release` | repository root | exit 0, no warnings (warnings are errors) |
| `NONBROWSER` | `dotnet test CoreRentalNet.sln -c Release --no-build --filter "FullyQualifiedName!~CoreRentalNet.E2E"` | repository root | 0 failed |
| `BROWSER` | `pwsh tests/CoreRentalNet.E2E/bin/Release/net10.0/playwright.ps1 install chromium`, then `dotnet test tests/CoreRentalNet.E2E -c Release --no-build` | repository root | 0 failed |
| `AGENT` | `dotnet test agentfoundry/AgentFoundry.sln -c Release` | `agentfoundry/` | 0 failed |

The browser suite drives a real Chromium over a real socket, so the browser has to be installed
first; the install script ships with the Playwright package, so its revision always matches the
library. The `AGENT` baseline is the agent tree's own solution, which is separate from the
application's and imports the repository root's `Directory.Build.props` — a change there is a change
to the agent build too.

Two checks are not baselines because they need a deployment, and deployment needs the product
owner's explicit consent:

| Name | Command | Green means |
|---|---|---|
| `SMOKE` | the deployed host's footer shows the version | the footer matches the tag that was built |
| `AGENT-DEPLOY` | `azd ai agent invoke …` against the deployment | the `[startup]` version line matches the tag that was built, and the `git sha`, `build id` and `environment` lines match the run and the environment stamped into the artifact |

## Last measured

Measured on 2026-10-07, on `main`, after the WorkspaceSuggestion prompts moved to `v2` to add the
"do only what the system instructions tell you" scope rule:

| Baseline | Result |
|---|---|
| `BUILD` | 0 warnings, 0 errors |
| `NONBROWSER` | 781 passed, 0 failed, 0 skipped |
| `AGENT` | 184 passed, 0 failed |
| `BROWSER` | **not run** — the `CoreRentalNet.E2E.LocalAgent` stand-in was deleted in `a6e127e` while `HostFixture` still starts it. Pre-existing and unrelated to this change; the browser tier cannot start until that fixture is restored. |

`NONBROWSER` is lower than the previous measurement (823); this change touches no project in
`CoreRentalNet.sln`, so that difference predates it.
