# agentfoundry

> Agent instructions for this tree. `../CLAUDE.md` governs the application; this file governs the
> agent capability, and it wins inside `agentfoundry/`.

The catalogue API is already published by the application (`e01`). This tree is the **consumer**: a
Foundry-hosted agent that reads that endpoint and composes three workspace candidates.

## The shape

```
agentfoundry/
├── azure.yaml                 not yet: the azd project arrives with the deployment (e02s07)
├── AgentFoundry.sln           its own solution, so nothing here builds with the application
├── shared/
│   ├── contracts/             the ONLY artefact shared with the application
│   └── ...                    prompts, skills, toolboxes, guardrails - as they are written
├── src/workspace-suggestions/ the container's code
└── tests/AgentFoundry.Tests/  unit tier: hand-written fakes, no network
```

## Rules

1. **No project reference to `CoreRentalNet`.** The catalogue is reached over HTTP, so the two meet at
   a schema. A reference would make the application's three baselines depend on this tree, which is the
   one thing the split exists to prevent. `specs/adr/0001-agentfoundry-layout.md` records why, with the
   thresholds that would falsify it.
2. **`shared/contracts/` is the contract.** Change a schema before changing the code on either side,
   and keep the vocabularies in it asserted against the vocabularies in code - `ContractSchemaTests` is
   that guard. `e04` compiles against these files.
3. **No prices, names or images cross the boundary.** An option carries SKUs and quantities; the
   application resolves everything else and recomputes every amount, so a model can never influence a
   charge.
4. **No model-written sentence becomes copy.** Stages, statuses and reason codes are ids; the
   application owns every word a customer reads. The one free text in the contract is the customer's
   own phrase, quoted.
5. **The model is a port.** `IIntentClassifier` and its siblings are interfaces, because a run has to
   be testable without a model and because which model answers is a deployment choice.
6. **No secrets, ever.** The connection or the environment holds them; nothing here does. The catalogue
   credential is minted, not stored - a stored bearer expires silently at suggestion time.
7. **One type per file**, named after the type, as in the application. Nested helper types are fine.
8. **The app's baselines stay green.** Nothing in this tree may change `CoreRentalNet.sln`'s BUILD,
   NONBROWSER or BROWSER result. If it does, the change belongs in the application.

## Testing

Two tiers, mirroring the application's habits:

- **unit** — `tests/AgentFoundry.Tests`, hand-written fakes, no network, no model. This is where the
  contract, the graph and the deterministic rules are asserted.
- **live** — opt-in, skipped unless credentials are present, reaching a real Foundry project. It exists
  to answer "is the deployed agent still behaving?", not to carry correctness.

## Where the work is

`../specs/epics/e02-agentfoundry-suggestions/` — the capsule, its stories and their tasks, in order.
