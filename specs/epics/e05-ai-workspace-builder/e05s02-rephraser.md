# e05s02 — The sentence becomes a specification

**type:** feat
**risk:** P0
**context:** agent
**bcps:** 5
**status:** passing

## Context

The rephraser is the first of the two agents and the one that makes the second tractable: it turns an
unstructured sentence into an explicit `WorkspaceSpec` — which slots the customer cares about, what
each is for, and the ceiling they named if they named one. Without it the suggestor would reason about
intent and selection at once, and neither could be graded alone.

It receives the whole catalogue like everything else in the run, so its specification can be grounded
in what actually exists.

## Requirements

#### ADDED: the rephraser agent

A profile whose instruction is a versioned file (`prompts/rephraser.v1.md`) and whose output format is
`ChatResponseFormat.ForJsonSchema<WorkspaceSpec>()`. Tool-less.

#### ADDED: the specification

A **typed envelope with natural-language purpose**: per slot the slot name, the quantity, and a short
purpose in ordinary words — *"a large, stable surface for two monitors"*. Plus the customer's **stated
monthly ceiling when they gave one**, and any free-text constraints. The envelope is validatable and
loggable; the purpose stays in words, because that is where "close to their query" lives and the
catalogue's own vocabulary is too product-shaped to express intent.

It carries **no SKU, no price and no product name**, and **no band label** — the application labels.

#### ADDED: the two verdicts

Either a specification, or the typed **not-a-workspace** verdict with a one-line reason. The verdict is
a result, not an error: a customer who types "write me a poem" has not triggered a failure.

#### ADDED: prompt versioning

`EmbeddedInstructionSource` loads the prompt and exposes its version, so the run record can name
what produced a suggestion (`e05s09`). The file is **embedded into the assembly at build**, so a hosted
agent cannot start without its instructions, and the loader takes the **file name** as an argument so a
second agent is a roster entry rather than an edit to a constant.

## Zoom-Out

- **Module purpose:** intent extraction. It decides what the customer wants, never which product
  satisfies it.
- **Callers:** the workflow (`e05s03`).
- **Contracts to preserve:** `WorkspaceSpec` is the only thing it hands on; a specification naming a
  SKU would collapse the separation this story exists to create.

## Steps

1. Write `prompts/rephraser.v1.md` and the loader that versions and embeds it.
   → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
2. Add the rephraser profile with typed structured output and keyed DI registration.
   → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
3. Tests AIWB-05 to AIWB-08: a vague sentence, no invented product, an off-topic verdict, and a stated
   ceiling. → verify: `dotnet test agentfoundry/AgentFoundry.sln --nologo`
