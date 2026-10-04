# Baselines

The four measurements a step is finished against, and what they are right now. A step is done only when
these are green, so the reds are named one by one rather than averaged into a number: what the next person
needs is not the total, it is whether the work changed any of them.

Measured on `8783ebf`, the commit before this file.

## BUILD

`dotnet build CoreRentalNet.sln`, Debug and Release: **0 warnings, 7 errors**.

Re-verified on the working tree after the workspace node-trace change: still 0 warnings and the same seven
errors below. The change is in `agentfoundry/`, and `CoreRentalNet.sln` references no project there, so this
baseline cannot move with it.

All seven are in `tests/CoreRentalNet.IntegrationTests/AgentInvocationHeaderTests.cs` (`CS0117`, four
facts). The test calls `RunScopeAccessToken.CarryTheTokenOf` and reads `RunScopeAccessToken.AccessToken`,
and the type has neither: it was flattened to a settable `Current` over a `RunContext` record. The test
came first and the type changed under it, so which shape survives is an open decision rather than a defect
to paper over - and until it is taken, this baseline cannot be green.

## NONBROWSER

**670 passed, 2 failed**, over the eight projects that run:

| Project | Result |
|---|---|
| Host | **257 passed** |
| Workspace | 81 passed |
| Architecture | **60 passed, 2 failed** |
| Rentals | 94 passed |
| Catalog | 70 passed |
| CatalogIngestion | 39 passed |
| BehaviourLock | 37 passed |
| BuildingBlocks | 32 passed |

`CoreRentalNet.IntegrationTests` **cannot run at all**: its project is the seven BUILD errors above. It was
141 tests at the last recording.

Both Architecture failures are the same unfinished decision:

- `OneTypePerFileTests` - `RunScopeAccessToken.cs` declares `RunContext` and the scope in one file, and the
  rule is one type per file, named after the file.
- `ProviderIsolationTests.The_adapter_still_names_them` - the guard still looks for
  `MicrosoftFoundryWorkspaceSuggestionAgentAdapterFactory`, which the provider rename replaced, so the rule
  it guards is passing by selecting nothing.

Against the recording in `specs/bugs/BUG-001.md`: Host 236 → 257, Workspace 80 → 81, AGENT 129 → 163. Those
differences are the suites growing, not a measurement of any one change.

Host is five over the last recording, from the catalogue tools' own answer: a description is capped at 400 characters
for the tools and only for them, and the compact API answer keeps the full text.

## AGENT

`dotnet test agentfoundry/AgentFoundry.sln`: **163 passed, 0 failed**.

Nine over the previous recording, from the guardrail layer (`specs/archive/spikes/SPIKE-guardrails.md`): six
policy tests (the allow-list, the argument ceiling, the catalogue vocabulary, untrusted-data redaction, the
recorder, and that a denied guard stops the chain) and three for the validator's new budget ceiling. The four
before those were the catalogue-argument fix; the four before those were the observability change. This is the
only baseline the change moves, because the agent solution is not part of `CoreRentalNet.sln`.

## BROWSER

**Blocked, and blocked before this work**: `tests/CoreRentalNet.E2E.LocalAgent` holds only `obj/` and has no
project, so the fixture cannot start the local agent it needs. The last recording was 118 failures, every
one of them `Not built: …LocalAgent.dll`.
