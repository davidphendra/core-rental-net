# 0001 — The agent capability lives in `agentfoundry/`, in this repository, as a second solution

**Status:** accepted
**Date:** 2026-09-16
**Deciders:** product owner, engineer

## Context

A Foundry-hosted agent must be built, tested and deployed beside an existing .NET 10 application that
has three green baselines — `BUILD`, `NONBROWSER` (548 tests, 4.86 s) and `BROWSER` (103 tests) — and a
stated rule in its README: *"This application must never scale out."* The agent needs a container, an
`azd` project, a Foundry project and a model deployment. The application must not acquire any of them.

Measured on the repository as it stands:

| Metric | Value |
|---|---|
| Projects on disk / in `CoreRentalNet.sln` | 23 / 31 (31 = 23 projects + 8 solution folders) |
| Project-to-project reference edges | 57 |
| Pinned package versions | 19 |
| Full rebuild | 6.5 s |
| NONBROWSER suite | 548 passed, 0 failed, 4.86 s, **100 % offline** |
| Tracked config files that can hold a secret | 2 |
| Git-tracked files | 531 |

## Decision

The agent capability lives in **`agentfoundry/`** at the repository root, with its **own solution**
(`AgentFoundry.sln`) and its own **azd project** (`agentfoundry/azure.yaml`), in the **same git
repository**, **inheriting** the root `Directory.Build.props` and `Directory.Packages.props`.

The two areas meet at exactly **one artefact**: the suggestion contract schema under
`agentfoundry/shared/contracts/`. There is no project reference in either direction.

## Alternatives considered, and the metrics that decided it

| Alternative | Metric | Measured / estimated | Verdict |
|---|---|---|---|
| Agent projects inside `CoreRentalNet.sln` | External blockers on the app gate | **0 → ≥ 1** (a Foundry or Auth0 outage would fail the app's suite) | rejected |
| | Hermetic fraction of the app gate | 548/548 (100 %) → `548/(548+L)` | rejected |
| | Blast radius of an agent-only change | re-runs 548 unrelated tests | rejected |
| | Full rebuild | 6.5 s → ~6.5 s + agent build | **not a reason** — the delta is seconds |
| A separate git repository | Duplicated build-policy files | **3** (`global.json`, `Directory.Build.props`, `Directory.Packages.props`) | rejected |
| | Contract-drift window | 2 commits / 2 pull requests instead of 1 | rejected |
| | Docker build context | 3 files to copy in or the image cannot restore | rejected |
| Local copies of the build policy in `agentfoundry/` | Duplicated policy files | 2 | rejected |
| | Version sources | 19 pins in 1 file → 19 pins in 2 | rejected |
| Treating `e01`'s endpoint as a general product reference model | Payload size | see RET-6: 20,688 B ≈ 5,172 tokens, 34 % of it display fields | addressed in e03s02 |

**Honest note on performance:** adding one or two projects to a 6.5 s rebuild would have cost single-digit
seconds. The separation is **not** justified on build time, and should not be defended on it.

## Consequences

**Good**

- The application's three baselines cannot be moved by agent work. An agent-only change re-runs agent
  tests only.
- One build policy and one package-version source for both trees, enforced by the compiler: central
  pinning makes a `Version=` attribute in an agent `.csproj` a build error.
- A change to the catalogue contract and the agent that consumes it lands in one commit.
- The Dockerfile's context is the repository root, so the three policy files are present with no copies.

**Bad**

- One repository with two lifecycles, so CI needs path-filtered jobs and a reader must know which tree
  they are in.
- `agentfoundry/` inherits the root build policy, which is intended but does mean a repository-wide
  policy change touches the agent too.
- Contributing to `agentfoundry/` requires `azd`, a container runtime and (for the live tier) an Azure
  login — none of which the application requires.

## Falsification thresholds

The decision is wrong, and should be revisited, if any of these holds after implementation:

1. `dotnet build agentfoundry/AgentFoundry.sln` takes more than **2×** the application's full rebuild
   (i.e. > 13 s).
2. Any application project gains a reference to an agent project (`grep -rl "AgentFoundry" src/ --include=*.csproj`
   returns anything).
3. The application's NONBROWSER gate acquires an external dependency — its hermetic fraction falls below
   **100 %**.
4. More than one authoritative definition of the application↔agent contract exists
   (`find . -name "workspace-suggestion.result.schema.json" | wc -l` ≠ 1).

Checked as:

```bash
! grep -rl "AgentFoundry" src/ --include=*.csproj
find . -name "workspace-suggestion.result.schema.json" | wc -l   # expect 1
git ls-files agentfoundry/ | xargs grep -lIl "client_secret\|Bearer " || echo clean
time dotnet build agentfoundry/AgentFoundry.sln -c Release
```

## References

- `specs/product/SCOPE_LATEST.yaml` — the initiative's scope
- `specs/epics/e03-catalogue-contract/epic.yaml`, `specs/epics/e02-agentfoundry-suggestions/epic.yaml`,
  `specs/epics/e04-ai-builder/epic.yaml`
- `CLAUDE.md` — the authority chain this decision must not contradict
