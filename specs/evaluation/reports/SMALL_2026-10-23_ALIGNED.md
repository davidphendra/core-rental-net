# Small workspace-suggestion evaluation — aligned pass

## Run

| Item | Value |
|---|---|
| Scope | `small` — 25 rows |
| Execution | sequential, `--concurrency 1` |
| Repetitions | 1 |
| Per-case timeout | 120 s |
| Timeout handling | dropped, no extension |
| Timeouts | **0** |
| Model | `Qwen3-Coder-30B-A3B-Instruct` |
| Report | `/tmp/aligned-small.json` |
| Traces | `/tmp/eval-aligned-small/` |

## Headline

| Measure | Value |
|---|---:|
| Cases | 25 |
| Case pass | **25/25 = 100.0%** |
| `answer_exact` | **24/25 = 96.0%** |
| `reranker_aligned` | **25/25 = 100.0%** |
| `constraint_compliance` | 25/25 |
| `retrieval_quality` | 24/25 |
| `raw_retrieval_recall` | 24/25 |
| `tool_call_accuracy` | 25/25 |
| `workflow_correctness` | 25/25 |

One case differs between exact and aligned:

| Case | Exact | Aligned | Reason |
|---|---|---|---|
| TS-009 | FAIL | **PASS** | Expected Dell `MONB000PB2KW` was not returned; reranker selected the duplicate-name Dell `MONB00112PRM` |

This is the intended aligned-pass behaviour.

---

# A. Fairness and outcome disparity

The fixture has no protected-group labels and no identity variants.

| Metric | Result | Reason |
|---|---|---|
| Demographic Parity Difference | **N/A** | no group labels |
| Disparate Impact Ratio | **N/A** | no group labels |
| Equal Opportunity Difference | **N/A** | no qualified labels by group |
| Equalized Odds Difference | **N/A** | no group labels or negative controls |
| Group Calibration Error | **N/A** | no confidence score and no groups |
| Worst-Group Performance | **100%** operational | every request shape passed |

Operational request-shape check:

| Request slice | Cases | Passed |
|---|---:|---:|
| Desk + Chair | 1 | 1 |
| Desk + Chair + Lamp | 1 | 1 |
| Desk + Chair + Monitor | 6 | 6 |
| Desk + Chair + Plant | 17 | 17 |

Operational pass difference: **0.0 percentage points**.

This is not a demographic fairness result.

**Finding A1** — no outcome disparity is observable in this small set, but there are no protected-group labels and no counterfactual pairs.

---

# B. Recommendation and retrieval bias

## Retrieval

| Measure | Value |
|---|---:|
| Expected SKU occurrences | 74 |
| Found in raw retrieval | **73/74 = 98.6%** |
| Recall@1 | **64/74 = 86.5%** |
| Recall@5 | **73/74 = 98.6%** |
| Recall@10 | **73/74 = 98.6%** |
| NDCG@10, per-slot binary relevance | **0.939** |
| Case-level raw recall | **24/25 = 96.0%** |

The only missing expected SKU is `MONB000PB2KW` in TS-009. The reranker selected the duplicate-name monitor `MONB00112PRM`, which is why the aligned pass succeeds while exact SKU fails.

## Recall and NDCG by request shape

| Request slice | n | Raw item recall | Recall@1 | Recall@5 | Recall@10 | NDCG@10 | Case pass |
|---|---:|---:|---:|---:|---:|---:|---:|
| Desk + Chair | 1 | 100% | 100% | 100% | 100% | 1.000 | 100% |
| Desk + Chair + Lamp | 1 | 100% | 100% | 100% | 100% | 1.000 | 100% |
| Desk + Chair + Monitor | 6 | 94.4% | 94.4% | 100% | 100% | 0.944 | 100% |
| Desk + Chair + Plant | 17 | 100% | 82.4% | 100% | 100% | 0.930 | 100% |

## Retrieval coverage

| Slot | Expected | Raw found | Selected | Composed |
|---|---:|---:|---:|---:|
| Desk | 25 | 25 | 25 | 25 |
| Chair | 25 | 25 | 25 | 25 |
| Plant | 17 | 17 | 17 | 17 |
| Monitor | 6 | 5 | 5 | 5 |
| Lamp | 1 | 1 | 1 | 1 |

## Recommendation diversity

| Measure | Value |
|---|---:|
| Composed lines | 74 |
| Distinct composed SKUs | **27** |
| Distinct composed SKU / line | 36.5% |
| Distinct selected SKUs | 33 |
| Categories exercised | Desk, Chair, Plant, Monitor, Lamp |

Final category exposure exactly matches the expected category exposure, so the category exposure difference is **0.0 percentage points**.

| Metric | Result |
|---|---|
| Group Exposure Difference | **N/A** — no demographic groups |
| Counterfactual Consistency | **N/A** — no counterfactual pairs |
| Attribute Sensitivity | **N/A** — no controlled attribute pairs |
| Recall@K / NDCG@K by demographic group | **N/A** — no groups |

**Finding B1** — retrieval is strong in the small slice, but the Dell monitor is a real duplicate-SKU coverage problem. The aligned pass correctly handles the reranker’s alternate selection.

---

# C. Agent reasoning and decision bias

| Metric | Result |
|---|---:|
| Decision Consistency Rate | **100%** — 9 duplicate groups, all outcomes consistent |
| Counterfactual Flip Rate | **N/A** — no counterfactual pairs |
| Unsupported Assumption Rate | **0/25** — rephraser categories matched the requested slots |
| Evidence Grounding Rate | **100% of issues** — 1/1 issue had both evidence fields |
| Tool Selection Disparity | **0 pp** — every requested component was searched |
| Constraint Violation Rate | **0/25 = 0%** |
| Reviewer Disagreement Rate | **1/25 = 4.0%** |

## Reviewer detail

| Measure | Value |
|---|---:|
| Reviewer decisions | 25 |
| Accepted | 24 |
| Rejected | 1 |
| Issues raised | 1 |
| Issue code | `SLOT_PURPOSE_UNSATISFIED` |
| Issue with both evidence fields | 1/1 |
| Reviewer accepted and case passed | 24/24 |
| Reviewer rejected and case passed | 1 — TS-017 |

TS-017 is a **reviewer false rejection**: the selected chair is `WorkPro Oceanic Mesh/Fabric Ergonomic High-Back Executive Chair`, which contains both `mesh` and `black`; the reviewer rejected it as “not a black mesh chair” because of the word “Executive”.

## Stage-level results

| Stage | Result |
|---|---|
| **Verifier** | 25/25 accepted; 0 false refusals |
| **Rephraser** | 25/25 valid; 0 category mismatches |
| **Retriever** | 24/25 cases had every expected SKU; 73/74 expected SKUs found |
| **Builder / reranker** | 25/25 aligned; 73/74 expected SKUs selected |
| **Composer** | 24/25 exact; 25/25 aligned pass |
| **Reviewer** | 24/25 accepted; one false rejection |

**Finding C1** — the only reasoning error visible in this slice is the reviewer false rejection at TS-017. The reranker alignment is clean.

---

# D. Language and content bias

No protected-group labels, identity prompts, annotation, toxicity classifier, or sentiment classifier exist.

| Metric | Result | Reason |
|---|---|---|
| Stereotype Incidence Rate | **N/A** | no annotation or classifier |
| Toxicity Rate by Group | **N/A** | no groups or classifier |
| Sentiment Disparity | **N/A** | no equivalent prompt pairs |
| Refusal Rate by Group | **N/A** by group; overall **0/25 = 0%** | no groups |
| Quality-of-Service Gap | **N/A** by demographic group | no groups |
| Uncertainty Calibration Gap | **N/A** | no confidence output or group labels |

Operational quality-of-service gap across request shapes: **0.0 percentage points**.

**Finding D1** — no language or content bias claim is supportable from 25 unlabelled cases.

---

# Overall findings

1. **Small result is 25/25 (100%)** under the aligned pass policy, with zero timeout drops.
2. **Exact-SKU result is 24/25 (96%)**; TS-009 is the only exact failure.
3. **TS-009 is the intended aligned pass**: the expected Dell monitor was missing from raw retrieval, and the reranker selected a duplicate-name Dell monitor.
4. **Retrieval is 98.6% item-level / 96.0% case-level**; the only miss is `MONB000PB2KW`.
5. **All 25 cases are reranker-aligned**; no reranker-order failure occurs in this slice.
6. **Reviewer quality is the only visible stage defect**: one false rejection at TS-017.
7. **A/D group fairness metrics remain uncomputable** without protected labels and counterfactual pairs.

# Artifacts

```text
specs/evaluation/reports/SMALL_2026-10-23_ALIGNED.md
/tmp/aligned-small.json
/tmp/eval-aligned-small/
```
