# e05s05 — The application can ask the agent and understand the answer

**type:** feat
**risk:** P0
**context:** app
**bcps:** 5
**status:** passing

## Context

The application must be able to call the agent without knowing that Foundry exists, and must keep
passing its suite with the network unplugged. That is one port, one real adapter, one
no-agent-configured implementation, and a hand-written fake — the shape every other external dependency
in this codebase already has.

## Requirements

#### ADDED: the port and its implementations

`ISuggestionAgent` with three implementations:

- `FoundrySuggestionAgent` — the real one, on the official Foundry/MAF client, authenticating with
  `DefaultAzureCredential`. It is the **only** class that touches an Azure type, and the **only** place
  a credential is resolved.
- `NoAgentConfigured` — the feature flag is off or the endpoint is empty; yields *unavailable*.
- A fake in tests — deterministic, offline, no credential.

#### ADDED: the call

The adapter builds a **`FoundryAgent`** — a MAF `AIAgent` backed by the remote hosted agent:

```
agentEndpoint = <projectEndpoint>/agents/<agentName>/endpoint/protocols/openai
agent         = new AIProjectClient(projectEndpoint, DefaultAzureCredential()).AsAIAgent(agentEndpoint)
stream        = agent.RunStreamingAsync(payload, cancellationToken: ct)
```

Configuration therefore carries the **project endpoint and the agent name**, never a per-agent URL —
the endpoint is derived. The adapter yields the stream onward as `Narrative` and `Completed`.

#### ADDED: reading the terminal answer, not the first one

The adapter reads the **last** top-level JSON object of the agent's response, not the first. `e05s03`
established why: a sequential workflow's response carries **both** agents' answers in order — the rephraser's
specification first, the suggestor's result last — and deserializing the first object yields the specification,
which fails the result contract. Neither `chainOnlyAgentResponses` nor
`includeWorkflowOutputsInResponse: false` avoids it without a worse cost.

This code cannot be shared with the agent: the two trees have no reference to each other, so the extraction
lives here and is tested here.

The application's side of the call needs **three** packages, pinned in its own central
`Directory.Packages.props` at the same versions as the agent's: `Microsoft.Agents.AI.Foundry`
(`FoundryAgent`), `Azure.AI.Projects` and `Azure.Identity`. It needs neither `.Foundry.Hosting` nor
`.Workflows` — those are the agent's alone.

#### ADDED: no session, ever

A run passes **no session, or a fresh one per run** — never a reused one. The hosted agent is a
singleton shared across customers, so a reused session would carry one customer's request into the
next customer's run. A test asserts it rather than a comment claiming it.

#### ADDED: the request the application builds

The compact projection **as it exists** — `sku`, `name`, `category`, `subCategory`, `pricePerMonth`,
`description`, `metadata` — built **in process** from the catalogue the application already loaded,
together with the slot rules, the capacities, and the customer's stated ceiling when there is one. Only
the query and this projection cross to Foundry.

#### ADDED: the payload hash

A hash of the projection **actually sent**, computed per run and carried on the result so the run record
can identify what the suggestion was drawn from (`e05s09`). Hashing what was sent — not the file —
means a change to the projection itself is captured too.

#### ADDED: the contract test

A test pinning the application's DTOs to `agentfoundry/shared/contracts/*.schema.json`, so the one
shared artefact cannot drift silently.

## Zoom-Out

- **Module purpose:** the application's only knowledge of the agent. It translates both ways and owns
  the failure vocabulary.
- **Callers:** the streamed run (`e05s07`).
- **Contracts to preserve:** the port's shape is the fake's shape; no Azure type escapes the adapter; an
  unreachable agent is *unavailable*, which is different from a *refusal*.

## Steps

1. Add `ISuggestionAgent`, its settings, and `NoAgentConfigured`.
   → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
2. Implement `FoundrySuggestionAgent`: derive `agentEndpoint` from the project endpoint and agent name,
   build the `FoundryAgent` with `DefaultAzureCredential`, and stream with `RunStreamingAsync` — no key
   in configuration.
   → verify: `dotnet build CoreRentalNet.sln -v q --nologo`
3. Run with no session, or a fresh one per run, and assert it.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~SuggestionAgentTests"`
4. Build the projection and the request, and hash what was sent.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo --filter "FullyQualifiedName~SuggestionAgentTests"`
5. Tests AIWB-17 to AIWB-19 and AIWB-47, including the contract pin.
   → verify: `dotnet test tests/CoreRentalNet.Host.Tests --nologo`

## Outcome

Built and verified offline. `ISuggestionAgent`, `SuggestionAgentSettings`, `NoAgentConfigured`,
`FoundrySuggestionAgent`, `FoundryAgentFactory`, the two wire records, the event hierarchy, the payload and
its hash, and `TerminalJsonObject`; registration is `builder.AddSuggestionAgent()` in the composition root.
`tests/CoreRentalNet.Host.Tests/SuggestionAgentTests.cs` and `SuggestionContractTests.cs` — 12 tests, no
network, no credential, no Foundry.

Three things worth recording:

- **AIWB-16 moved to `e05s08`.** It asks for prices recomputed from the catalogue, which is validation, and
  this story stops at the agent's typed result. The matrix was wrong, and the scenario was reassigned rather
  than implemented in the wrong place.
- **AIWB-47 is a shape, not an observation.** There is no session argument in the call and no session field on
  the adapter, so a session cannot be carried. The test observes the consequence instead: two identical runs
  send byte-identical payloads. Stated plainly so the scenario is not read as stronger than it is.
- **The agent name has a default.** A deployment supplies the project endpoint; the name falls back to
  `core-rental-workspace-suggestion-agent`, because the agent's own `azure.yaml` fixes it. The endpoint is the
  one setting that must be supplied, and its absence hides the feature rather than opening it.

**It also found a defect this story did not introduce.** The application's `PackagePolicyTests` — "package
versions live in `Directory.Packages.props` only" — scans every `.csproj` under the repo root, so it had been
failing since the agent tree landed at `e05s01`: the agent pins its own versions by design (ADR 0003, no
central file). The test now excludes `agentfoundry/`, with the reason written beside it. The application's
suite had not been run during `e05s01`–`e05s03`, which is why it went unnoticed; it is green now.

**A gap in this story's tests was found at `e05s04` and closed.** `TerminalJsonObject` exists to read the
**last** top-level JSON object *because the response carries two* — and it had only ever been tested with
**one**. The behaviour it was written for was uncovered. Capturing the real wire at `e05s04` produced the
authentic two-object text, and `SuggestionAgentTests` now feeds it through the adapter and asserts the
specification is not mistaken for the result. Worth recording as a lesson: the fixture was too easy, and
only contact with the real wire exposed it.
