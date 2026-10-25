# CasePassPolicy Removal and Reranker-Result Plan

Status: proposed. No code changes in this plan.

## Decision

Remove `CasePassPolicy` as the case-result authority.

The case result must follow the reranker result:

```text
casePass = final reranker result accepted
```

Judge, exact-answer, constraint, retrieval, and workflow metrics remain diagnostics. They do not override the reranker result.

## Required semantics

The reranker result is authoritative only when it is the result of the **final completed reranker stage**.

```text
reranker result exists
AND reranker result is non-empty
AND every returned SKU came from the supplied candidate pool
=> casePass = true

otherwise
=> casePass = false
```

A stale reranker result from an earlier attempt must not pass a later failed attempt.

Therefore the final-attempt inference must also be fixed:

```text
evaluation/.../Evidence/StreamEventFacts.cs
  FinalAttempt = completed?.CompletedAttemptCount ?? 1
```

must become an attempt value that cannot select attempt 1 when no completed event exists. Use an explicit unknown/0 value and let `FinalRerankSelections` be empty.

## Files to change

```text
evaluation/src/CoreRentalNet.Evaluation/Evaluation/CasePassPolicy.cs
evaluation/src/CoreRentalNet.Evaluation/Evaluation/EvaluationReportFactory.cs
evaluation/src/CoreRentalNet.Evaluation/Evidence/StreamEventFacts.cs
evaluation/src/CoreRentalNet.Evaluation/Evidence/EvaluationContext.cs
evaluation/src/CoreRentalNet.Evaluation/Evaluation/EvaluationReport.cs
evaluation/src/CoreRentalNet.Evaluation/Evaluation/RepetitionResult.cs
```

## Repetition policy

`CasePassPolicy` also owns the repetition majority rule:

```csharp
passCount >= Math.Min(2, repetitionCount)
```

Removing the class means deciding the replacement explicitly.

Recommended conservative rule:

```text
CasePass = all repetitions have an accepted final reranker result
```

Alternative:

```text
CasePass = any repetition has an accepted final reranker result
```

The second can hide instability and is not recommended.

## Result fields

Keep these as diagnostics:

```text
answer_exact
constraint_compliance
retrieval_quality
raw_retrieval_recall
tool_call_accuracy
workflow_correctness
judge_evaluated
judge_pass
timed_out
```

They must not be required for case pass once the reranker result is authoritative.

Do not silently empty failure codes for a passing case. If the reranker result passes but another metric fails, report that difference.

## Other silent processing in the reranker path

These are the production-side transformations that can make the reranker result differ from what the reranker emitted.

### 1. Exact-match override — most important

File:

```text
agentfoundry/.../Stages/DeterministicPolicies/WorkspaceComponentProductSelectionPolicy.cs
```

`ExactMatches` runs before the reranker ranking:

```csharp
var exactMatches = ExactMatches(...);
var candidates = exactMatches.Count > 0 ? exactMatches : ranked;
```

`ExactMatches` takes every product in the retrieved pool whose name contains an uppercase search term and uses that list instead of the reranker output.

Effect:

```text
reranker says A
uppercase search term matches B
=> B is selected
```

This directly contradicts “respect the reranker result”.

Plan: remove the `ExactMatches` override and always use the reranker order.

### 2. Reranker output truncated to three

Same file:

```csharp
.Take(_maximumSelectedProductsPerComponent)
```

The first three reranker products per component reach the composer. Products four and later are silently discarded.

Plan: keep the three-product composer bound, but document it as a deliberate bound; do not treat the truncated list as the reranker’s full result.

### 3. Unknown SKUs dropped

Same file:

```csharp
ResolveToPooledProduct(...).OfType<SelectedWorkspaceComponentProduct>()
```

A reranker SKU that is not in the pool disappears.

This is a necessary safety guard, but it means the reranker result is not passed through literally.

### 4. Duplicate SKUs collapsed

Same file:

```csharp
.DistinctBy(selection => selection.RetrievedProduct.Sku)
```

Repeated SKUs in the reranker response are removed.

### 5. Pool de-duplication across components

File:

```text
agentfoundry/.../Stages/DeterministicPolicies/WorkspaceComponentProductPoolPolicy.cs
```

`seenStockKeepingUnits` drops a product from a later component if the same SKU was seen in an earlier component.

This is before the reranker, but it changes what the reranker can return.

### 6. Reranker result truncation in evaluation

File:

```text
evaluation/src/CoreRentalNet.Evaluation/Evidence/WorkspaceTraceProjector.cs
```

```csharp
foreach (var assessment in assessments.Take(_maximumSelectedProductsPerComponent))
```

The evaluation reads only the first three reranker selections per component.

This is an evaluation artifact, not the production selection policy, but it can make `reranker_aligned` disagree with the full reranker response.

### 7. Unknown reranker components ignored

File:

```text
evaluation/src/CoreRentalNet.Evaluation/Evidence/EvaluationComponentMapping.cs
```

An unknown component key causes the evidence to be skipped, not to fail.

### 8. Malformed reranker JSON becomes no result

File:

```text
evaluation/src/CoreRentalNet.Evaluation/Evidence/WorkspaceTraceProjector.cs
```

`TryReadRanking` returns `null` on invalid JSON. The stage is then treated as having selected nothing.

### 9. Stale final attempt

File:

```text
evaluation/src/CoreRentalNet.Evaluation/Evidence/StreamEventFacts.cs
```

When no completed event exists, `FinalAttempt` becomes `1`. An earlier reranker result can therefore be treated as the final one.

### 10. Reranker alignment is narrow by design

File:

```text
evaluation/src/CoreRentalNet.Evaluation/Evaluation/RerankerAlignment.cs
```

It only checks:

```text
non-empty
and every selected SKU came from the supplied tool answers
```

It does not require:

- all requested components to be selected;
- the expected SKU;
- the final composer to use the SKU;
- the expected order.

If the reranker result is authoritative, define whether “non-empty subset” is enough. If yes, state it. If not, add the missing conditions explicitly.

## Removal sequence

1. Add an explicit `FinalAttempt` unknown value and make missing completion produce no final reranker.
2. Remove `CasePassPolicy` use from `EvaluationReportFactory`.
3. Set per-repetition `CasePass` from the final accepted reranker result.
4. Define the repetition-level pass rule.
5. Keep judge and metric failures as diagnostics.
6. Remove `ExactMatches` from `WorkspaceComponentProductSelectionPolicy`.
7. Ensure the composer receives the reranker order, after the explicit three-product bound.
8. Remove `CasePassPolicy` tests, or replace them with the new policy tests.
9. Re-run the tiny scope before half/full.

## Acceptance

```text
A final reranker result with no unknown SKUs => case pass
No final reranker result or unknown SKU => case fail
Timeout before final reranker => fail through missing final reranker, not through CasePassPolicy
Judge pass/fail does not change case pass
Exact answer does not override the reranker result
```

## Open decision

The only policy decision that remains is the repetition rule:

```text
all repetitions must pass
or
any repetition may pass
```

The first is recommended.

## Reranker-authority removal tasks

These tasks are in addition to the CasePassPolicy removal and are still pending.

### Contract

- [ ] R1 — Define the authoritative reranker result: final completed reranker, non-empty, all SKUs from the pool.
- [ ] R2 — Specify that composer, reviewer, validator, and workflow must not change, replace, omit, or reject that result.
- [ ] R3 — Specify that no post-reranker retry may rerun the reranker.

### Composer

- [ ] R4 — Remove the composer prompt rule that returns an empty array when budgets or capacity cannot be met.
- [ ] R5 — Remove the composer prompt rule allowing “fewer setups” or omission of selected products.
- [ ] R6 — Remove the composer budget and capacity veto from the composition decision.
- [ ] R7 — Require the composer to materialise every selected reranker product.
- [ ] R8 — Make budget or capacity conflicts a reported conflict, not a reason to drop the result.

### Reviewer

- [ ] R9 — Convert `workspace-setup-reviewer.v6.md` to a report-only observer.
- [ ] R10 — Remove `isAcceptable` rejection authority and all reviewer reject codes from the decision path.
- [ ] R11 — Remove `WorkspaceSetupReviewEvidencePolicy` as a gate.
- [ ] R12 — Remove reviewer success/retry routing from `WorkspaceSetupReviewExecutor`.
- [ ] R13 — Keep reviewer observations as diagnostics only.

### Retry and workflow

- [ ] R14 — Remove reviewer-triggered retry.
- [ ] R15 — Remove validator-triggered retry.
- [ ] R16 — Remove `WorkspaceSetupRetryDecisionPolicy` dependency on review acceptance.
- [ ] R17 — Remove the graph edge `Reviewer -> RetryDecision`.
- [ ] R18 — Remove the graph edge `Validator -> RetryDecision` for semantic or budget rejection.
- [ ] R19 — Route success directly from the composer to the success node.
- [ ] R20 — Move `WorkspaceSetupCandidateApprovedEvent` publication to the composer/reranker boundary.

### Validator

- [ ] R21 — Convert `WorkspaceSetupCandidateStructureValidator` to report-only.
- [ ] R22 — Remove `MONTHLY_CEILING_EXCEEDED`, `QUANTITY_ABOVE_CAPACITY`, `SLOT_NOT_REQUESTED`, and `SKU_NOT_RETRIEVED` as gates.
- [ ] R23 — Keep validator violations as diagnostics.

### Selection and pool

- [ ] R24 — Remove `Take(_maximumSelectedProductsPerComponent)` truncation from the post-reranker selection.
- [ ] R25 — Do not silently drop unknown reranker SKUs; fail the reranker contract instead.
- [ ] R26 — Do not silently collapse duplicate reranker SKUs; define the contract.
- [ ] R27 — Remove cross-component SKU de-duplication from the pre-reranker pool, or declare it as a hard contract.
- [ ] R28 — Confirm `ExactMatches` remains removed from `WorkspaceComponentProductSelectionPolicy`.

### Documentation and tests

- [ ] R29 — Update `WorkspaceSetupReviewResult`, `WorkspaceSetupCandidateApprovedEvent`, and workflow documentation to remove reviewer authority.
- [ ] R30 — Remove or rewrite tests that expect reviewer, validator, or composer rejection to control the workflow.
- [ ] R31 — Remove or rewrite tests that expect reranker selections to be truncated silently.
