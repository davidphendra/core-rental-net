# Commit plan for the uncommitted e05 replacement

The working tree held the whole `e05` replacement as one change. This is how it was committed, why it was
cut where it was, and what each slice can and cannot prove on its own. It exists because the risk is real
and unrecorded: 17,883 deletions against 479 insertions in tracked files, plus two whole projects
untracked, replacing work that was already committed on this branch.

## The slices, in the order they were made

### 1. The record

`specs/**` — ADR 0002 (the builder), ADR 0003 (the agent repository layout), the `e05` epic capsule,
this plan, the release plan's risk register, and the removal of the capsules and verification documents
that `e05` supersedes.

**Why first:** nothing compiles `specs/`. This slice cannot break a baseline, so it is the one that is
free to make. It also puts the design on the record *before* the code that implements it, which is the
order the rest of this repository's history uses.

**What it cannot prove alone:** nothing about the code. It is prose and YAML.

### 2. The agent tree

`agentfoundry/**` — the new layout, prompts, contracts, its own solution, its test project, and the
`azure.yaml` the deployment reads.

**Why it can stand alone:** `CoreRentalNet.sln` does not reference the agent tree and the agent tree does
not reference the application — that is ADR 0003's whole claim, and it is what makes this slice
independently buildable and testable through `agentfoundry/AgentFoundry.sln`.

**What it cannot prove alone:** the *application's* half of the shared contract. The four schemas under
`agentfoundry/shared/contracts/` are the one artefact both trees read, and the application's DTOs are
validated against them. The contracts move here, with the tree that publishes them; the assertion that the
application still matches them is the next slice's.

### 3. The application replacement

`src/**`, `tests/**`, `CoreRentalNet.sln`, `Directory.Packages.props`, `README.md` — the module-level AI
design removed, the host's replacement installed, the whole test tree rebuilt around it.

**Why it cannot be cut finer, and this is the useful part of this document.** Three couplings were checked
rather than assumed:

- **The shared contract.** `tests/CoreRentalNet.Host.Tests/SuggestionContractTests.cs` validates the
  application's DTOs against `agentfoundry/shared/contracts/*.json`, and the agent tree's own tests validate
  against the same files. A commit carrying new DTOs without the new contracts fails; so does one carrying
  the contracts without the DTOs. The contracts bind both trees, so neither tree can move without them.
- **A member's accessibility.** `Agents/SuggestionRequestBuilder.cs` needs `CompactCatalogProjection.Item`,
  which is `private` in the committed version and public only in the working tree. Committing the builder
  without the projection change is a compile error, not a style question.
- **Removed tests against removed code.** `ApplyComposition`, `Contracts/Suggestion` and the module services
  are deleted, and so are the tests that reference them. Committing either half alone leaves a suite that
  cannot build, which is worse than a large commit.

So the application replacement is one commit by necessity rather than convenience. Saying so here is the
point: a slice plan that pretended otherwise would produce commits that fail a clean checkout.

## Baselines

Each slice was committed on the same working tree that was already verified, so the numbers do not move
between them. Recorded once, at the end of the third slice:

- **BUILD** — `CoreRentalNet.sln` and `agentfoundry/AgentFoundry.sln`, both clean, 0 warnings, 0 errors
- **NONBROWSER** — 636 passed, 0 failed across 8 projects
- **AGENT** — 63 passed, 0 failed
- **BROWSER** — 104 passed, 1 skipped, 0 failed
- **TOTAL** — 803 passed, 1 skipped, 0 failed

## What is deliberately not done

- **Nothing is pushed.** The slices are local commits on `e03-catalogue-contract`.
- **Main is not merged or rebased.** The branch is 24 commits ahead of `main` and `main` is a strict
  ancestor; that is recorded as risk `r02` in `specs/release-plan.yaml` rather than acted on, because it is
  a review question and not a commit one.
- **No slice claims to have been reviewed.** They are commit boundaries, not approvals.
