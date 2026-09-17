# Agent Foundry

The agent that turns *"a quiet corner for two monitors and a decent chair"* into three workspace
candidates, drawn from the catalogue the application already publishes.

It lives beside the application in the same repository but in its own solution, so nothing it needs -
a container, an `azd` project, a Foundry project, a model deployment - can move the application's
baselines. The reasoning, with the measurements behind it, is in
`../specs/adr/0001-agentfoundry-layout.md`.

## The design in one line

Four agents in one Microsoft Agent Framework workflow, in one container: a **verifier** that refuses
anything that is not about a workspace, a **rephraser** that turns the request into a specification, a
**suggestor** that reads the catalogue and composes three candidates across low, middle and high, and a
**reviewer** that approves them or sends findings back.

The suggestor is the only node that reads the catalogue, and the application is the only place that
decides what a customer may see.

## What exists

| | |
|---|---|
| `shared/contracts/` | the request, the stage event and the result, as JSON Schema - the only artefact shared with the application |
| `src/workspace-suggestions/` | the contract's records, the stage and reason-code vocabularies, the intent-classifier port, the verifier and the workflow that runs it |
| `tests/AgentFoundry.Tests/` | the contract, the schema vocabularies and one run end to end, with a hand-written classifier |

## What does not exist yet

Each is a story in `../specs/epics/e02-agentfoundry-suggestions/`, and each is listed here so nothing
looks finished when it is not:

- **The rephraser, the suggestor and the reviewer.** The graph runs the verifier alone today. The nodes
  attach to it as they are written, because a stub that approved nothing would produce a result a
  caller could not tell from a real one.
- **The model.** `IIntentClassifier` is a port; the adapter that answers it with a Foundry model arrives
  with the deployment.
- **The container and its transport.** No `Dockerfile`, no `azure.yaml`, no Responses host. The
  packages it needs are preview, and there is no endpoint here to verify them against.
- **Prompts, skills, toolboxes, guardrails.** `shared/` holds the contracts and nothing else so far.

## Run it

```bash
dotnet build AgentFoundry.sln
dotnet test AgentFoundry.sln
```

Neither needs a network, a model or a credential: the whole unit tier runs against hand-written fakes.
