# Tiny workspace-suggestion evaluation — aligned pass

## Run

| Item | Value |
|---|---|
| Scope | `tiny` — 5 rows |
| Cases | TS-001, TS-041, TS-081, TS-121, TS-161 |
| Execution | sequential, `--concurrency 1` |
| Repetitions | 1 |
| Per-case timeout | 120 s |
| Timeout handling | drop, no extension |
| Model | `Qwen3-Coder-30B-A3B-Instruct` |
| Prompt set | verifier v3, rephraser v7, retriever v5, reranker v4, composer v3, reviewer v4 |
| Report | `/tmp/aligned-tiny2.json` |
| Traces | `/tmp/eval-aligned-tiny2/` |

## Result

| Measure | Value |
|---|---:|
| Rows | 5 |
| Passed | **5** |
| Pass rate | **100.0%** |
| Dropped at 120 s | **0** |
| `reranker_aligned` | **5/5** |
| `answer_exact` | **5/5** |
| `constraint_compliance` | **5/5** |
| `retrieval_quality` | **5/5** |
| `raw_retrieval_recall` | **5/5** |
| `tool_call_accuracy` | **5/5** |
| `workflow_correctness` | **5/5** |

All five cases were both exact-SKU passes and reranker-aligned passes.

| Case | Request | Result |
|---|---|---|
| TS-001 | Desk + Chair | PASS |
| TS-041 | Desk + Chair + Plant | PASS |
| TS-081 | Desk + Chair + Plant | PASS |
| TS-121 | Desk + Chair + Plant | PASS |
| TS-161 | Desk + Chair + Monitor | PASS |

---

# A. Fairness and outcome disparity

The fixture has no protected-group labels and no identity variants.

| Metric | Result | Reason |
|---|---|---|
| Demographic Parity Difference | **N/A** | no group labels |
| Disparate Impact Ratio | **N/A** | no group labels |
| Equal Opportunity Difference | **N/A** | no qualified/relevant group labels |
| Equalized Odds Difference | **N/A** | no group labels or negative controls |
| Group Calibration Error | **N/A** | no confidence score and no groups |
| Worst-Group Performance | **100.0%** operational | all five request shapes passed |

Operational request-shape check:

| Request slice | Cases | Passed |
|---|---:|---:|
| Desk + Chair | 1 | 1 |
| Desk + Chair + Plant | 3 | 3 |
| Desk + Chair + Monitor | 1 | 1 |

The operational pass difference is **0.0 percentage points**. This is not a demographic fairness result.

**Finding A1** — no outcome disparity is observable in the tiny set, but the set is too small and has no protected-group labels. No demographic claim can be made.

---

# B. Recommendation and retrieval bias

## Recall@K

All 14 expected SKU occurrences were retrieved and selected.

| Measure | Value |
|---|---:|
| Expected SKU occurrences | 14 |
| Found in raw retrieval | **14/14 = 100%** |
| Recall@1 | **12/14 = 85.7%** |
| Recall@5 | **14/14 = 100%** |
| Recall@10 | **14/14 = 100%** |
| NDCG@10, per-slot binary relevance | **0.947** |

The two rank-2 items were the desks in TS-081 and TS-121. They were still selected and composed correctly.

## Recall and NDCG by request shape

| Request slice | n | Recall@1 | Recall@5 | Recall@10 | NDCG@10 |
|---|---:|---:|---:|---:|---:|
| Desk + Chair | 1 | 100% | 100% | 100% | 1.000 |
| Desk + Chair + Plant | 3 | 77.8% | 100% | 100% | 0.918 |
| Desk + Chair + Monitor | 1 | 100% | 100% | 100% | 1.000 |

## Retrieval coverage

| Slot | Expected | Raw found | Selected | Composed |
|---|---:|---:|---:|---:|
| Desk | 5 | 5 | 5 | 5 |
| Chair | 5 | 5 | 5 | 5 |
| Plant | 3 | 3 | 3 | 3 |
| Monitor | 1 | 1 | 1 | 1 |

Coverage is **100%** for every slot exercised in this scope.

## Recommendation diversity

| Measure | Value |
|---|---:|
| Composed lines | 14 |
| Distinct composed SKUs | 14 |
| Distinct composed SKU / line | 100% |
| Categories exercised | Desk, Chair, Plant, Monitor |

No SKU was repeated across the five workspaces. No category over-exposure is visible.

| Metric | Result |
|---|---|
| Group Exposure Difference | **N/A** — no demographic groups |
| Counterfactual Consistency | **N/A** — no counterfactual pairs |
| Attribute Sensitivity | **N/A** — no controlled attribute pairs |
| Group Recall@K / NDCG@K | **N/A** — no demographic groups |

**Finding B1** — retrieval is healthy in this tiny slice: every expected SKU was in raw results, selected, and composed.

---

# C. Agent reasoning and decision bias

| Metric | Result |
|---|---:|
| Decision Consistency Rate | **N/A** — no duplicate sentence groups in the tiny set |
| Counterfactual Flip Rate | **N/A** — no counterfactual pairs |
| Unsupported Assumption Rate | **0/5** — rephraser emitted exactly the requested categories |
| Evidence Grounding Rate | **N/A** — reviewer raised no issues; 0 issues to audit |
| Tool Selection Disparity | **0 pp** — every requested component got both searches |
| Constraint Violation Rate | **0/5 = 0%** |
| Reviewer Disagreement Rate | **0/5 = 0%** |

## Stage-level results

| Stage | Result |
|---|---|
| **Verifier** | 5/5 accepted; 0 false refusals |
| **Rephraser** | 5/5 valid; categories matched the expected slots exactly |
| **Retriever** | 14/14 expected SKUs retrieved; both tools called for every component |
| **Builder / reranker** | 14/14 expected SKUs selected |
| **Composer** | 14/14 expected SKUs composed; 5/5 setups exact |
| **Reviewer** | 5/5 accepted; 0 issues |

Reviewer detail:

| Measure | Value |
|---|---:|
| Valid decisions | 5 |
| Accepted | 5 |
| Rejected | 0 |
| Issues raised | 0 |
| Reviewer accepted and case passed | 5/5 |

**Finding C1** — no upstream stage loss is visible in the tiny scope. The reranker kept the supplied products, and the aligned gate agrees with the exact gate.

---

# D. Language and content bias

No protected-group labels, identity prompts, annotation, toxicity classifier, or sentiment classifier exist.

| Metric | Result | Reason |
|---|---|---|
| Stereotype Incidence Rate | **N/A** | no annotation or classifier |
| Toxicity Rate by Group | **N/A** | no groups or classifier |
| Sentiment Disparity | **N/A** | no equivalent prompt pairs |
| Refusal Rate by Group | **N/A** by group; overall **0/5 = 0%** | no groups |
| Quality-of-Service Gap | **N/A** by demographic group | no groups |
| Uncertainty Calibration Gap | **N/A** | no confidence output or group labels |

The operational quality-of-service gap across request shapes is **0.0 percentage points**.

**Finding D1** — no language or content bias claim is supportable from five unlabelled cases.

---

# Overall findings

1. **Tiny result is 5/5 (100%)** under the aligned pass policy, with no timeout drops.
2. **Exact and aligned gates agree on this slice**: all five were exact-SKU and reranker-aligned.
3. **Retrieval is complete**: 14/14 expected SKUs found, selected, and composed.
4. **No stage-level loss is visible** in the verifier, rephraser, retriever, builder, composer, or reviewer.
5. **A/D group fairness metrics remain uncomputable** without protected labels.
6. **B/C operational metrics show no disparity** on this tiny set, but five cases cannot establish fairness.

# Artifacts

```text
specs/evaluation/reports/TINY_2026-10-23_ALIGNED.md
/tmp/aligned-tiny2.json
/tmp/eval-aligned-tiny2/
```
