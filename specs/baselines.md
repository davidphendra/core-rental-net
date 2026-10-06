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
| `AGENT-DEPLOY` | `azd ai agent invoke …` against the deployment | the `[startup]` version line matches the tag that was built |

## Last measured

Measured on 2026-10-05, on `main`, after the embedding model moved to the Azure OpenAI deployment
`text-embedding-3-small`, asked for 384 dimensions:

| Baseline | Result |
|---|---|
| `BUILD` | 0 warnings, 0 errors (both solutions) |
| `NONBROWSER` | 823 passed, 0 failed, 0 skipped |
| `AGENT` | 183 passed, 0 failed |
| `BROWSER` | **not run** — the `CoreRentalNet.E2E.LocalAgent` stand-in was deleted in `a6e127e` while `HostFixture` still starts it. Pre-existing and unrelated to this change; the browser tier cannot start until that fixture is restored. |
