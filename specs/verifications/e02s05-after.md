# e02s05 — nothing is returned that was not checked

**Date:** 2026-09-16
**Branch:** `e03-catalogue-contract`

## What was run

| Check | Before | After |
|---|---|---|
| BUILD `agentfoundry/AgentFoundry.sln` | 0 warnings, 0 errors | **0 warnings, 0 errors** |
| TESTS `agentfoundry/AgentFoundry.sln` | 25 passed | **30 passed, 0 failed, 4 skipped** |
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

## Blocked — the retry loop does not run

**The retry edge from the reviewer back to the rephraser never fires.** The workflow ends after the
reviewer on the first attempt, whatever the review says, so `exhausted` is produced at attempt 1 and
attempt 2 is never reached. The four tests that assert the loop are **kept and skipped**, naming this
record, rather than deleted or adjusted to match the broken behaviour.

What is known:

- The graph, the conditional edge and the condition all compile, and the condition is written the way
  the framework's own loop sample writes it.
- The first edge — verifier to rephraser, also conditional and also typed — **does** fire, so the
  problem is not conditions or typed edges in general.
- Removing the reviewer from `WithOutputFrom` did not make the loop run; the run then ended without a
  judgement at all.
- Microsoft.Agents.AI.Workflows 1.21.0 is the current stable line; the samples that loop use a plain
  `AddEdge(from, to)` with **no** condition, which the framework does not allow here because a retry
  must be decided from the review.

**Next step, and the one to try first:** give the retry its own untyped edge by making the reviewer's
output and the rephraser's input the same concrete type, so the condition is not needed on the loop
edge and the reviewer decides by sending or not sending. That is a message-shape change, not a
redesign: the pieces it needs — the attempt count, the findings and the request — all travel already.

## What the run corrected

1. **The validator needed a receipt.** Checking the composition against itself proves nothing, so the
   ordered SKUs per slot travel from the suggestor with the options. That is what catches a picker
   returning the wrong position — the one failure that changes what a customer is offered while every
   other check still passes.
2. **The specification had to carry the request.** A retry builds a specification from the same slot
   rules as the first attempt, and threading the request separately produced a node holding a `Query`
   property and an empty slot list.
3. **`Tier.All.IndexOf` does not exist on an `IReadOnlyList`.** Positions are read through a
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
