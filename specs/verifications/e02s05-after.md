# e02s05 — nothing is returned that was not checked

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `agentfoundry/AgentFoundry.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| TESTS `agentfoundry/AgentFoundry.sln` | 25 passed | **34 passed, 0 failed, 0 skipped** |
| BUILD `CoreRentalNet.sln` | unmoved | **0 warnings, 0 errors — unmoved** |
| NONBROWSER `CoreRentalNet.sln` | 570 passed | **570 passed, 0 failed — unmoved** |

## What changed

- **`Review/ReviewValidator`** — the computable checks, in code: every slot the specification names is
  filled exactly once with the quantity it asked for, no line names a slot the specification did not,
  and each pick is the one the tier dictates once the receipt is re-derived.
- **`Review/IReviewComposition`** — the judgement, returning findings and never an option.
- **`Review/Reviewer`** — the gate: computable checks first, the judgement only if they pass, and the
  oscillation guard, which compares the specification of this attempt against the previous one.
- **`Workflow/ReviewerExecutor`** — the fourth node, counting its own attempts.
- **`Candidates` carries the ordered SKUs per slot** — the receipt the validator re-derives the tiers
  from. Without it the validator could only check a composition against itself.
- **`Specification` carries the request** — a retry must be built from the same slot rules the first
  attempt was, and the request is what holds them.
- The graph's fourth node, the retry edge, and the result mapping: `ok` when approved, `exhausted`
  with the candidates and the findings when not, `rejected` when the verifier refused.

## Measured effect

| Case | Result |
|---|---|
| A pick that is not the one the tier dictates | refused in code, **without asking the judgement** |
| A slot the composition left out | refused in code |
| A sound composition | reaches the judgement, and is approved when nothing objects |
| The same specification twice | the guard reports it, and the loop stops rather than repeating |
| An accepted request | all four stages stream, and the result is `ok` with candidates |

## The retry loop — found and fixed

**Two typed edges leading into the same executor do not both arrive.** The framework binds an
executor's input from the first edge it is given, so the verifier's edge to the rephraser took effect
and the reviewer's retry edge — carrying a different type — was **silently dropped**. The workflow then
ended after the reviewer on the first attempt, whatever the review said.

What ruled the other explanations out, in order:

- The graph and the condition compile, and the condition is written the way the framework's own loop
  sample writes it.
- The verifier's edge to the rephraser is *also* conditional and *also* typed, and it fires — so
  conditions and typed edges are not the problem in themselves.
- Removing the reviewer from `WithOutputFrom` did not make the loop run.
- **Replacing the condition with `attempt < 2` — as simple as a condition can be — still did not make
  it fire.** That is what showed the edge was not being evaluated at all, rather than evaluating false.

The fix is one concrete message type, `Round`, flowing round the loop: the verifier sends one with no
specification, the reviewer sends one with the composition and its verdict, and both edges carry the
same type so both arrive. The four tests that assert the loop are no longer skipped.

## What the run corrected

1. **Two typed edges into one executor silently drop the second.** The most expensive finding in this
   epic: the graph compiled, the tests passed, and the loop simply did not happen. It was found by
   making the condition trivial and watching it still not fire.
2. **The validator needed a receipt.** Checking the composition against itself proves nothing, so the
   ordered SKUs per slot travel from the suggestor with the options. That is what catches a picker
   returning the wrong position — the one failure that changes what a customer is offered while every
   other check still passes.
3. **The specification had to carry the request.** A retry builds a specification from the same slot
   rules as the first attempt, and threading the request separately produced a node holding a `Query`
   property and an empty slot list.
4. **`Tier.All.IndexOf` does not exist on an `IReadOnlyList`.** Positions are read through a
   `Tier.PositionOf` helper, which also refuses to guess: an unknown tier is -1, not "low".

## Not done, deliberately

- **The semantic judgement has no implementation.** `IReviewComposition` is a port; the adapter that
  answers it with a model arrives with the deployment, like the other two.
- **Structural findings do not take a separate repair path.** R3 intends a computable failure to be
  repaired in code rather than sent back to be rephrased; as it stands both kinds of finding travel the
  same retry edge. The guard means a structural retry stops immediately rather than spending model
  calls, so the cost R3 worried about is avoided even though the repair is not automatic.
- **Attempt 3 is unreachable with a deterministic rephraser.** The same request produces the same
  specification, so the guard stops at attempt 2. The third attempt is available to a rephraser that
  varies, which is what the model-backed one will do.
