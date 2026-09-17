# e02s05 — Nothing is returned that was not checked

**type:** feat
**risk:** P0
**context:** infra
**bcps:** 8
**status:** failing

## Context

The reviewer faces two structurally different kinds of failure, and giving them one repair path was
the mistake this story exists to avoid:

| Class | Examples | Nature |
|---|---|---|
| **Deterministic** | a slot's pick is not the one P1 dictates; slot sets differ between candidates; a quantity exceeds capacity; a mandatory slot is missing; a SKU does not exist; a criterion has no disposition | computable exactly |
| **Semantic** | the specification no longer serves the request; a rationale cites a criterion the user never made; the request was quietly narrowed | judgement |

A **code validator** owns the first class and repairs it in code — no model call, no loop. Only the
second class enters the **rephrase loop**, bounded at three attempts.

## Requirements

#### ADDED: a deterministic validator that gates

Every invariant above is checked in code. A failure is repaired deterministically (recompute the tier,
drop the over-capacity unit) and recorded — it never consumes a loop attempt and never reaches the
rephraser. The validator's verdict **cannot be overridden by the model**.

#### ADDED: an LLM semantic check, findings only

The model judges only whether the specification still serves the request, and emits **structured
findings**. It never writes a specification (AGENT-3): the rephraser remains the sole author, so the
slot vocabulary and the inferred marking are enforced in one place.

#### ADDED: a bounded loop with a disclosed exhaustion

At most **three attempts**. A retry whose specification is **equivalent to one already attempted** is
refused by an oscillation guard, comparing the normalised specification rather than the prose. On
exhaustion the last candidates are returned with `status: exhausted` and the outstanding findings, so
a pathological request becomes an assertable outcome rather than a hang (L2).

## Zoom-Out

- **Module purpose:** the gate. It approves, repairs, or sends findings — it produces no content.
- **Callers:** the workflow, which either returns its verdict or re-enters the rephraser.
- **Contracts to preserve:** the result schema; the rule that a deterministic repair leaves no trace
  in the loop counters; the rephraser's sole authorship of the specification.

## Steps

1. Implement the deterministic validator over the candidates and the specification. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~ValidatorTests"`
2. Implement the in-code repairs and assert they consume no attempt. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~DeterministicRepairTests"`
3. Implement the semantic check emitting findings only, and assert it cannot return a specification.
   → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~FindingsOnlyTests"`
4. Wire the loop: findings to the rephraser, three attempts maximum. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~LoopTests"`
5. Add the oscillation guard over the normalised specification. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~OscillationTests"`
6. Implement `exhausted` with the outstanding findings. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo --filter "FullyQualifiedName~ExhaustionTests"`
7. Unit tests AGT-14 … AGT-17. → verify: `dotnet test agentfoundry/tests/AgentFoundry.Tests --nologo`

## Verification Script (Step-by-Step)

1. Feed the validator a candidate whose pick is one position away from P1's → repaired in code, no
   additional model call, run still `ok`.
2. Feed it slot sets that differ between candidates → repaired, not looped.
3. Feed it a request no composition can satisfy → three attempts, then `status: exhausted` with the
   findings and the last candidates.
4. Feed it two successive equivalent specifications → the guard refuses the second before the third
   attempt.
5. Confirm the total model calls for a three-attempt run match the expected count.

## Test matrix

| ID | Scenario | Level |
|----|----------|-------|
| AGT-14 | A wrong tier pick is repaired in code with no further model call | unit |
| AGT-15 | A semantic failure re-enters the rephraser and stops after three attempts | unit |
| AGT-16 | An equivalent retry is refused by the oscillation guard | unit |
| AGT-17 | Exhaustion returns the candidates with `exhausted` and the findings | unit |

## Out of scope

- Prompt quality and the rephraser's wording (`e02s03`).
- Catalogue retrieval and tiering (`e02s04`).
- The golden-query evaluation dataset (deferred; first rows captured by hand).

## Risks

- **Asking a language model to check arithmetic it cannot see.** The whole reason the validator
  exists; the reviewer has no catalogue access, and it must not be asked to check prices or SKUs.
- **A loop that cycles.** The oscillation guard is what makes "three attempts" mean three *different*
  attempts rather than three of unknown value.
- **Treating exhaustion as failure.** It is a result: the candidates are still returned, with the
  caveat the application renders.

## Acceptance criteria

- AGT-14 … AGT-17 pass with a scripted model.
- A deterministic failure never increments the attempt counter.
- The model cannot return a specification.
- The application's three baselines are untouched.
