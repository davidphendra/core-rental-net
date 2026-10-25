# Workspace Suggestion Evaluation

Scope: **Tiny** (5 of 200 rows)  
Rows: **5**  
Executions: **5**  
Case pass: **5/5 (100.0 %)**  
Judge: **1/2 passed (50.0 %)**

## Timing

| Case | Wall | Node total | Unaccounted | Model | Tool | Guardrail | Host | Slowest stage |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| TS-001 | 103578.5 ms | 20684.0 ms | 82894.5 ms | 20322.9 ms | 174.3 ms | 1.9 ms | 2.1 ms | catalogue-candidate-reranker |
| TS-041 | 107261.0 ms | 28658.5 ms | 78602.5 ms | 28381.6 ms | 248.7 ms | 0.4 ms | 5.0 ms | catalogue-candidate-reranker |
| TS-081 | 106386.3 ms | 28014.3 ms | 78371.9 ms | 27802.6 ms | 176.3 ms | 0.5 ms | 2.2 ms | catalogue-candidate-reranker |
| TS-121 | 106269.6 ms | 28144.8 ms | 78124.9 ms | 27989.3 ms | 122.4 ms | 0.5 ms | 3.2 ms | catalogue-candidate-reranker |
| TS-161 | 107414.9 ms | 28713.9 ms | 78701.0 ms | 28504.7 ms | 188.7 ms | 0.4 ms | 2.8 ms | catalogue-candidate-reranker |

### Stage timing

| Case | Stage | Total | Model calls | Tool planning | Final structured | Tool exec | Tool calls | Unattributed |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| TS-001 | workspace-request-verifier | 520.4 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 33.1 ms |
| TS-001 | workspace-requirement-rephraser | 3022.4 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 29.8 ms |
| TS-001 | catalogue-product-retriever | 5722.3 ms | 2 | 3165.4 ms | 2292.9 ms | 174.3 ms | 4 | 89.8 ms |
| TS-001 | build-candidate-product-pool | 7.8 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 7.8 ms |
| TS-001 | catalogue-candidate-reranker | 8047.8 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 8.2 ms |
| TS-001 | workspace-setup-composer | 3350.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 5.9 ms |
| TS-001 | complete-workspace-suggestion-success | 12.2 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 12.2 ms |
| TS-041 | workspace-request-verifier | 375.0 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.9 ms |
| TS-041 | workspace-requirement-rephraser | 4614.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.6 ms |
| TS-041 | catalogue-product-retriever | 8577.0 ms | 2 | 4436.9 ms | 3878.6 ms | 248.7 ms | 6 | 12.8 ms |
| TS-041 | build-candidate-product-pool | 0.6 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.6 ms |
| TS-041 | catalogue-candidate-reranker | 11250.6 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.0 ms |
| TS-041 | workspace-setup-composer | 3833.4 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.3 ms |
| TS-041 | complete-workspace-suggestion-success | 7.0 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 7.0 ms |
| TS-081 | workspace-request-verifier | 343.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.1 ms |
| TS-081 | workspace-requirement-rephraser | 3481.1 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 7.7 ms |
| TS-081 | catalogue-product-retriever | 8339.5 ms | 2 | 4320.3 ms | 3831.5 ms | 176.3 ms | 6 | 11.4 ms |
| TS-081 | build-candidate-product-pool | 1.2 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.2 ms |
| TS-081 | catalogue-candidate-reranker | 11634.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.6 ms |
| TS-081 | workspace-setup-composer | 4203.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.4 ms |
| TS-081 | complete-workspace-suggestion-success | 10.1 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 10.1 ms |
| TS-121 | workspace-request-verifier | 325.2 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.2 ms |
| TS-121 | workspace-requirement-rephraser | 3631.4 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.1 ms |
| TS-121 | catalogue-product-retriever | 8682.7 ms | 2 | 4132.7 ms | 4410.0 ms | 122.4 ms | 6 | 17.6 ms |
| TS-121 | build-candidate-product-pool | 0.3 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.3 ms |
| TS-121 | catalogue-candidate-reranker | 11515.2 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.6 ms |
| TS-121 | workspace-setup-composer | 3981.8 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.2 ms |
| TS-121 | complete-workspace-suggestion-success | 8.1 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 8.1 ms |
| TS-161 | workspace-request-verifier | 372.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.4 ms |
| TS-161 | workspace-requirement-rephraser | 4513.3 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.0 ms |
| TS-161 | catalogue-product-retriever | 8734.8 ms | 2 | 4551.1 ms | 3983.2 ms | 188.7 ms | 6 | 11.8 ms |
| TS-161 | build-candidate-product-pool | 0.4 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.4 ms |
| TS-161 | catalogue-candidate-reranker | 10720.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.3 ms |
| TS-161 | workspace-setup-composer | 4371.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.3 ms |
| TS-161 | complete-workspace-suggestion-success | 0.3 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.3 ms |

## Metrics

| Metric | Case pass rate |
|---|---:|
| `answer_exact` | 60.0 % |
| `constraint_compliance` | 100.0 % |
| `raw_retrieval_recall` | 100.0 % |
| `reranker_aligned` | 100.0 % |
| `retrieval_quality` | 80.0 % |
| `tool_call_accuracy` | 100.0 % |
| `workflow_correctness` | 100.0 % |

## Stability

| Repetitions | Rows |
|---|---:|
| 1/1 | 5 |

## Judge

| Case | Pass | Confidence | Attempts | Reason |
|---|---|---:|---:|---|
| TS-001 | PASS | 0.95 | 1 | The final setup includes a mahogany desk (Alera VA216630MY Valencia Series Straight Front Desk Shell, Mahogany) and a green velvet armless chair (ZSARTS Velvet Armless Desk Chair, Green). Both items match the requested attributes—mahogany finish for the desk and green velvet armless design for the chair—satisfying the customer’s explicit request. The quantities are correct, and all selected SKUs are within the provided candidate pool. No required attributes are missing. |
| TS-121 | FAIL | 0.95 | 1 | The final desk is a HON Mod Desk Shell in Mahogany, which is not black and is not a mobile desk. The customer explicitly requested a black mobile desk, so the desk requirement is not satisfied. |

## A. Fairness and outcome disparity

| Metric | Stage | Status | Value | n | Reason |
|---|---|---|---:|---:|---|
| Demographic Parity Difference | - | NotApplicable | - | 0 | No protected-group labels exist in the fixture. |
| Disparate Impact Ratio | - | NotApplicable | - | 0 | No protected-group labels exist in the fixture. |
| Equal Opportunity Difference | - | NotApplicable | - | 0 | No qualified labels or protected-group labels exist in the fixture. |
| Equalized Odds Difference | - | NotApplicable | - | 0 | No protected-group labels or negative controls exist in the fixture. |
| Group Calibration Error | - | NotApplicable | - | 0 | No protected-group labels or per-group confidence values exist in the fixture. |
| Worst-Group Performance | - | NotApplicable | - | 0 | No protected-group labels exist in the fixture. |

## B. Recommendation and retrieval bias

| Metric | Stage | Status | Value | n | Reason |
|---|---|---|---:|---:|---|
| Group Exposure Difference | - | NotApplicable | - | 0 | No protected-group labels exist in the fixture. |
| Recall@K by Group | - | NotApplicable | - | 0 | No protected-group labels exist in the fixture. |
| NDCG@K by Group | - | NotApplicable | - | 0 | No protected-group labels exist in the fixture. |
| Recommendation Diversity | - | Computed | 0.929 | 14 | - |
| Category Exposure | - | Computed | 0.000 | 14 | - |
| Retrieval Coverage | - | Computed | 1.000 | 14 | - |
| Counterfactual Consistency | - | NotApplicable | - | 0 | No counterfactual pairs exist in the fixture. |
| Attribute Sensitivity | - | NotApplicable | - | 0 | No controlled attribute pairs exist in the fixture. |

## C. Agent reasoning and decision bias

| Metric | Stage | Status | Value | n | Reason |
|---|---|---|---:|---:|---|
| Verifier Refusal Rate | verifier | Computed | 0.000 | 5 | - |
| Decision Consistency Rate | - | InsufficientData | - | 0 | No duplicate sentence groups exist in the selected scope. |
| Counterfactual Flip Rate | - | NotApplicable | - | 0 | No counterfactual pairs exist in the fixture. |
| Unsupported Assumption Rate | rephraser | Computed | 0.000 | 5 | - |
| Evidence Grounding Rate | builder | Computed | 1.000 | 16 | - |
| Tool Selection Disparity | retriever | Computed | 0.000 | 3 | - |
| Constraint Violation Rate | builder | Computed | 0.000 | 5 | - |
| Reviewer Disagreement Rate | reviewer | NotApplicable | - | 0 | No reviewer decisions were found in the trace. |

### C.1 Stage breakdown

| Stage | Metric | Status | Value | n |
|---|---|---|---:|---:|
| verifier | Verifier Refusal Rate | Computed | 0.000 | 5 |
| rephraser | Unsupported Assumption Rate | Computed | 0.000 | 5 |
| builder | Evidence Grounding Rate | Computed | 1.000 | 16 |
| retriever | Tool Selection Disparity | Computed | 0.000 | 3 |
| builder | Constraint Violation Rate | Computed | 0.000 | 5 |
| reviewer | Reviewer Disagreement Rate | NotApplicable | - | 0 |

## D. Language and content bias

| Metric | Stage | Status | Value | n | Reason |
|---|---|---|---:|---:|---|
| Stereotype Incidence Rate | - | NotApplicable | - | 0 | No stereotype annotation or classifier exists in the evaluation harness. |
| Toxicity Rate by Group | - | NotApplicable | - | 0 | No toxicity classifier or protected-group labels exist in the evaluation harness. |
| Sentiment Disparity | - | NotApplicable | - | 0 | No equivalent prompt pairs or sentiment classifier exist in the fixture. |
| Refusal Rate by Group | - | NotApplicable | - | 0 | No protected-group labels exist in the fixture. |
| Quality-of-Service Gap | - | NotApplicable | - | 0 | No protected-group labels exist in the fixture. |
| Uncertainty Calibration Gap | - | NotApplicable | - | 0 | No protected-group labels or per-group confidence values exist in the fixture. |

## Failing rows

| Row | Case | Stability | Answer F1 | Retrieval recall | Failure codes |
|---:|---|---|---:|---:|---|

## All rows

| Row | Case | Stability | Reranker aligned | Judge | Case pass | Failure codes |
|---:|---|---|---|---|---|---|
| 1 | TS-001 | 1/1 | YES | PASS | PASS | ANSWER_WRONG_SKU:Desk |
| 41 | TS-041 | 1/1 | YES | - | PASS |  |
| 81 | TS-081 | 1/1 | YES | - | PASS |  |
| 121 | TS-121 | 1/1 | YES | FAIL | PASS | ANSWER_WRONG_SKU:Desk<br>ANSWER_WRONG_SKU:Chair<br>RETRIEVAL_MISS:DSKB07Z74NMV<br>RETRIEVAL_MISS:CHAB09RLYPZ1<br>JUDGE_REJECTED |
| 161 | TS-161 | 1/1 | YES | - | PASS |  |
