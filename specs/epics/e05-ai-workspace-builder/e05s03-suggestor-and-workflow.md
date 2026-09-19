# e05s03 — The specification becomes candidates

**type:** feat
**risk:** P0
**context:** agent
**bcps:** 8
**status:** passing

## Context

The suggestor is the second agent and the only one that composes. It takes the specification and the
catalogue the request carried, and proposes genuinely different setups that respect the slot
capacities and the customer's stated ceiling when there is one.

The two agents are then composed into **one** workflow and published as a **single** agent, so the host
serves one thing and the application never learns there are two.

## Requirements

#### ADDED: the suggestor agent

A profile whose instruction is `prompts/suggestor.v1.md` and whose output is the typed result. It is
tool-less; the catalogue arrives **inside the request**, which is what keeps the safety posture
structural.

#### ADDED: the sequential workflow

`Workflows/WorkspaceSuggestionWorkflow`, built with
`AgentWorkflowBuilder.BuildSequential([rephraser, suggestor])` and published with
`.AsAIAgent(name: "core-rental-workspace-suggestion-agent")`. **Default chaining is required**: the
suggestor must keep seeing the request that carried the catalogue, and `chainOnlyAgentResponses: true`
would take it away.

#### ADDED: how many candidates

The agent proposes three where the catalogue supports three distinct answers, and **fewer where it does
not** — never padding a near-duplicate to reach three. It does **not** label them: the application sorts
by monthly total and labels by rank.

#### ADDED: stable identities

Each agent's `Id` and `Name` are defined once, in the roster. They are the executor identities a
checkpoint records, so renaming one is a resume-compatibility decision, not a rename.

#### ADDED: the workflow as the served agent

`Composition/WorkflowRegistration` publishes the workflow under the name the Foundry host resolves, so
`AddFoundryResponses()` can find it in keyed DI.

## Zoom-Out

- **Module purpose:** composition. It chooses products and quantities and states no price.
- **Callers:** the host, which serves the workflow as one agent.
- **Contracts to preserve:** candidates carry no price, no product name and no label; both agents remain
  tool-less; the graph stays a straight line until a story needs otherwise.

## RECORDED: the workflow's response carries both agents' answers

Writing AIWB-09 established something the story's wording did not anticipate, and it changes `e05s05`:

**A sequential workflow's response carries both agents' answers, in order — the rephraser's specification
first, the suggestor's result last.** `RunAsync<T>` deserializes the **first** top-level object, so it would
return the *specification* and fail against the result contract. The terminal answer has to be read as the
**last** object.

Two things were tried and do not avoid it: `chainOnlyAgentResponses: true` would make the response the
terminal output alone, but it also takes the catalogue away from the suggestor (AIWB-10), which is the whole
reason default chaining is required; and `AsAIAgent(..., includeWorkflowOutputsInResponse: false)` still
returns both, verified by running it.

The consequence is accepted rather than worked around, because the catalogue reaching the suggestor matters
more than a single-object response: **the application's adapter reads the last top-level object** (`e05s05`).
That code cannot be shared — the two trees have no reference to each other — so `e05s05` states it for itself,
and AIWB-09 here proves the order by index.

## Steps

1. Write `prompts/suggestor.v1.md` and the suggestor profile with the result schema.
   → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
2. Build the sequential workflow and publish it as one agent through keyed DI.
   → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
3. Register both agent identities in one place and prove they survive a rebuild.
   → verify: `dotnet test agentfoundry/AgentFoundry.sln --nologo`
4. Tests AIWB-09 to AIWB-12, including the catalogue-reaches-the-suggestor case.
   → verify: `dotnet test agentfoundry/AgentFoundry.sln --nologo`
