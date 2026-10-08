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

Measured on 2026-10-08, on `main`, after the composition root was rebuilt as a list of host steps and the
echo agent's name was completed from `echo-agent` to `echo-reverse-agent`:

| Baseline | Result |
|---|---|
| `BUILD` | 0 warnings, 0 errors |
| `NONBROWSER` | 797 passed, 0 failed, 0 skipped |
| `TOOL` | 42 passed, 0 failed, 0 skipped |
| `AGENT` | 199 passed, 0 failed |
| `BROWSER` | **not run** — the `CoreRentalNet.E2E.LocalAgent` stand-in was deleted in `a6e127e` while `HostFixture` still starts it. Pre-existing and unrelated to this change; the browser tier cannot start until that fixture is restored. |

`AGENT` rose from the recorded 184 by fifteen, to 199. Thirteen are the composition-root extraction: seven
pin the host's settings (the fallback default agent, the trim rule, the echo state, the disabled-echo refusal,
the transport bind, and the two presence fields), two the default-agent registration, three the startup report,
and one the `.env` refill rule. The remaining two were already in the tree and unrecorded when this change was
measured.

`BUILD` and `NONBROWSER` are unchanged at 797: `agentfoundry/` is a separate solution, and nothing in the root
tree reads it, so a change confined to it cannot move either. `TOOL` was re-measured and is unchanged. The
echo agent's rename touches `azure.yaml`, the pipeline, `appsettings.json`, `.env.example` and the README; it is
not exercised by any baseline, and `AGENT-DEPLOY` remains the check that would prove it on a deployment.
