# e02s02 — one request in, one typed result out

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract` (the agent tree is new; a dedicated branch would carry no history)

## What was run

| Check | Result |
|---|---|
| BUILD `agentfoundry/AgentFoundry.sln` | **0 warnings, 0 errors** |
| TESTS `agentfoundry/AgentFoundry.sln` | **8 passed, 0 failed** |
| BUILD `CoreRentalNet.sln` | **0 warnings, 0 errors — unmoved** |
| NONBROWSER `CoreRentalNet.sln` | **570 passed, 0 failed — unmoved** |

The last two are the point of the layout: this story added a solution, two projects, three packages
and a schema directory, and the application's baselines did not move. `specs/adr/0001` names that as
the property to protect and gives the thresholds that would falsify it.

## What changed

- **`agentfoundry/`** — its own solution, `src/workspace-suggestions/`, `tests/AgentFoundry.Tests/`,
  `AGENTS.md`, `README.md`, `.gitignore`.
- **Three schemas** under `agentfoundry/shared/contracts/` — request, stage event, result. This is the
  artefact `e04` compiles against, which is why it lands first.
- **The C# contract** — request, slot rule, the message union, stage event, result, option, line,
  unevaluated criterion, finding; and the stage, status and reason-code vocabularies.
- **`IIntentClassifier`** — the port the verifier decides through.
- **`VerifierExecutor` and `SuggestionWorkflow`** — the first node, and the run that streams the
  contract's messages from it.
- **`Directory.Packages.props`** — `Microsoft.Agents.AI` and `Microsoft.Agents.AI.Workflows`, both
  stable at 1.21.0 and both pinned, with the reason they are referenced rather than left transitive.

## Measured effect

| | Before | After |
|---|---|---|
| Solutions in the repository | 1 | 2 |
| Projects in `agentfoundry/` | 0 | 2 |
| Artefacts shared with the application | 0 | **1** — the contract directory |
| Project references across the boundary | 0 | **0** |
| Application baselines | 570 / 0 | **570 / 0** |

## What the run corrected

1. **A four-node graph with three stubs would have shipped a lie.** A stub reviewer approves the empty
   composition a stub suggestor produced, so the run would answer `ok` with no candidates — a result a
   caller could not tell from a real one. The graph therefore runs the **verifier alone**, and the
   later nodes attach as they are written. `AGT-03` asserts that an accepted request streams its stage
   and **sends no result yet**, which is the honest state rather than a passing one.
2. **The .NET SDK now scaffolds `.slnx`.** The capsule names `AgentFoundry.sln`; `dotnet new sln
   --format sln` was needed to produce the classic format the application uses.
3. **The schema-vocabulary tests drilled one level short.** The first run failed all three, because the
   path reached the property and not its `enum` array. The guard is worth having exactly because it
   fails loudly when a vocabulary moves on one side only.
4. **The test project needed `using Xunit;`.** The template's implicit `<Using Include="Xunit" />` was
   dropped when the project file was rewritten to match the repository's shape; the repository's own
   test files import it explicitly, and now these do too.

## Not done, deliberately

- **The Responses transport, the container, `azure.yaml` and the model adapter.** All deferred to
  `e02s07`. The packages they need (`Microsoft.Agents.AI.Foundry`, `Microsoft.Agents.AI.Hosting`) are
  **preview**, and there is no Foundry endpoint in this environment to verify them against — so pinning
  them now would be an unverified dependency rather than progress.
- **The loop edge and the branch.** They arrive with the rephraser, which is the node they need.
- **`Microsoft.Extensions.AI` directly.** `Microsoft.Agents.AI` carries it, and nothing here names a
  type from it yet; the direct reference arrives with the adapter that does.
