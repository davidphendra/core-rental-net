# e02s03 — The request becomes a specification

**type:** feat
**risk:** P0
**context:** infra
**bcps:** 5
**status:** passing

## Context

The suggestor cannot reason about free text; it needs a **specification**: which slots the workspace
contains, how many units each holds, and which criteria the request makes. This story builds the
rephraser, which is also the **sole author** of that specification — the reviewer emits findings and
never rewrites it (AGENT-3), so the slot vocabulary, the capacity bounds and the inferred marking are
enforced in exactly one place.

The lookup itself is **code, not a model decision**: the rephraser normalises the request into
canonical intent phrases and a deterministic table maps those to slots. Only a **miss** reaches the
model, and only within the closed `SlotId` enum.

## Requirements

#### ADDED: the intent table and the slot set

A versioned table in `agentfoundry/shared/` maps intent phrases to slot sets — the same
rules-as-data pattern as `SlotRuleProvider`. A phrase with no match falls back to the model, which
must choose from the **seven-member `SlotId` enum**; the resulting set is marked
**`inferred: llm`** and the miss is **logged**.

The slot set is **fixed once and identical across all three candidates** (T2), so the three options
differ in what fills each slot, not in which slots exist.

#### ADDED: quantities, bounded by the request

Each slot carries a quantity, **defaulting to 1**, clamped to the `maxQuantity` the request supplied —
the application's `WorkspaceSlotSettings` stays the only place a capacity is defined. One product
fills a slot and is repeated by quantity; mixing products inside one slot is out of scope, because it
would make "low / middle / high" ambiguous for that slot.

## Zoom-Out

- **Module purpose:** the rephraser turns wording into structure. It holds no catalogue access
  (AGENT-4) and states no products.
- **Callers:** the workflow's loop, which re-enters it with accumulated findings.
- **Contracts to preserve:** the specification shape that `e02s02` defined; `SlotId`'s membership; the
  request's capacity table as the only capacity source.

## Steps

1. Add the intent table as data with its loader and validation. → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
2. Implement the rephraser: normalise the request to canonical phrases, look up the table, and build
   the specification. → verify: `dotnet build agentfoundry/AgentFoundry.sln -v q --nologo`
3. Add the miss path: classify into the closed `SlotId` enum, mark the set inferred, log the miss. →
   verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~IntentMissTests"`
4. Apply quantities, clamping to the capacity the request carries. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~QuantityTests"`
5. Accept the findings input so a retry is authored by the same component that authored attempt one.
   → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~FindingsIntakeTests"`
6. Unit tests AGT-04 … AGT-07. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~RephraserTests"`

## Verification Script (Step-by-Step)

1. Run the container locally with a scripted model.
2. Send *"an office setup for two monitors and a decent chair"* → the specification names the slots
   the table declares for that phrasing, with quantity 2 on the monitor slot.
3. Send a phrasing the table does not hold → the slot set is present, marked inferred, and a miss is
   logged.
4. Send a request asking for five monitors when the capacity is three → the quantity is three.
5. Confirm the same request produces the same slot set twice over. Comparing three candidates needs candidates, which arrive with `e02s04`.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AGT-04 | A declared phrase produces its slot set | unit |
| AGT-05 | A miss falls back to `SlotId`, marked inferred, and is logged | unit |
| AGT-06 | Quantity defaults to 1 and is clamped to the request's capacity | unit |
| AGT-07 | The same request produces the same specification, and one run asks once | unit |

## Out of scope

- Product selection and tiering (`e02s04`).
- Criteria disposition (`e02s04` maps them, `SAFE-1`/C3 governs the disclosure).
- Catalogue access: the rephraser has none.

## Risks

- **The model choosing a slot outside the catalogue's ability to fill.** Bounded by `SlotId` and by
  the catalogue covering every slot with 20 products after `e03s01`.
- **Two authors of one specification.** The reviewer must not write one; `e02s05` asserts that it
  emits findings only.
- **The inferred path becoming the normal path.** Logged per miss, so the table rotting is visible as
  a rate rather than discovered by a user.

## Acceptance criteria

- AGT-04 … AGT-07 pass; AGT-05 asserts the closed-enum property and that a miss is logged.
- A declared phrase is bit-for-bit reproducible; only a miss is model-dependent.
- The application's three baselines are untouched.
