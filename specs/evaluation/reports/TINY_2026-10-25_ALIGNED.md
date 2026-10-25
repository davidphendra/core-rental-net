# Workspace Suggestion Evaluation

Scope: **Tiny** (5 of 200 rows)  
Rows: **5**  
Executions: **5**  
Case pass: **3/5 (60.0 %)**  
Judge: **0/2 passed (0.0 %)**

## Timing

| Case | Wall | Node total | Unaccounted | Model | Tool | Guardrail | Host | Slowest stage |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| TS-001 | 180121.3 ms | 47499.5 ms | 132621.8 ms | 46866.8 ms | 422.1 ms | 1.8 ms | 8.0 ms | catalogue-candidate-reranker |
| TS-041 | 138612.2 ms | 34708.7 ms | 103903.5 ms | 34439.4 ms | 235.0 ms | 0.4 ms | 4.0 ms | catalogue-candidate-reranker |
| TS-081 | 129890.8 ms | 32564.2 ms | 97326.6 ms | 32239.6 ms | 293.6 ms | 0.5 ms | 4.2 ms | catalogue-candidate-reranker |
| TS-121 | 180113.8 ms | 53440.0 ms | 126673.8 ms | 52672.6 ms | 731.0 ms | 0.8 ms | 5.9 ms | catalogue-candidate-reranker |
| TS-161 | 127391.6 ms | 29773.4 ms | 97618.1 ms | 29563.2 ms | 184.2 ms | 0.4 ms | 2.5 ms | catalogue-candidate-reranker |

### Stage timing

| Case | Stage | Total | Model calls | Tool planning | Final structured | Tool exec | Tool calls | Unattributed |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| TS-001 | workspace-request-verifier | 3168.1 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 32.7 ms |
| TS-001 | workspace-requirement-rephraser | 3096.4 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 30.0 ms |
| TS-001 | catalogue-product-retriever | 6390.2 ms | 2 | 3788.0 ms | 2248.2 ms | 265.5 ms | 4 | 88.5 ms |
| TS-001 | build-candidate-product-pool | 7.0 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 7.0 ms |
| TS-001 | catalogue-candidate-reranker | 8368.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 11.0 ms |
| TS-001 | workspace-setup-composer | 4165.8 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 5.8 ms |
| TS-001 | validate-workspace-setup-structure | 6.1 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 6.1 ms |
| TS-001 | workspace-setup-reviewer | 4335.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 10.3 ms |
| TS-001 | decide-workspace-setup-retry | 2.4 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.4 ms |
| TS-001 | workspace-requirement-rephraser | 5399.5 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 3.3 ms |
| TS-001 | catalogue-product-retriever | 5496.5 ms | 2 | 2990.1 ms | 2338.9 ms | 156.6 ms | 4 | 10.9 ms |
| TS-001 | build-candidate-product-pool | 0.6 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.6 ms |
| TS-001 | catalogue-candidate-reranker | 7062.2 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.2 ms |
| TS-041 | workspace-request-verifier | 1845.6 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.1 ms |
| TS-041 | workspace-requirement-rephraser | 3976.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.9 ms |
| TS-041 | catalogue-product-retriever | 9306.9 ms | 2 | 4496.2 ms | 4562.7 ms | 235.0 ms | 6 | 12.9 ms |
| TS-041 | build-candidate-product-pool | 0.5 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.5 ms |
| TS-041 | catalogue-candidate-reranker | 10577.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.8 ms |
| TS-041 | workspace-setup-composer | 3811.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.0 ms |
| TS-041 | validate-workspace-setup-structure | 0.2 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.2 ms |
| TS-041 | workspace-setup-reviewer | 5177.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.7 ms |
| TS-041 | complete-workspace-suggestion-success | 11.1 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 11.1 ms |
| TS-081 | workspace-request-verifier | 843.2 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 6.5 ms |
| TS-081 | workspace-requirement-rephraser | 3397.3 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 7.7 ms |
| TS-081 | catalogue-product-retriever | 8234.3 ms | 2 | 4149.8 ms | 3780.8 ms | 293.6 ms | 6 | 10.0 ms |
| TS-081 | build-candidate-product-pool | 0.8 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.8 ms |
| TS-081 | catalogue-candidate-reranker | 11633.4 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.9 ms |
| TS-081 | workspace-setup-composer | 4399.8 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.2 ms |
| TS-081 | validate-workspace-setup-structure | 0.1 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.1 ms |
| TS-081 | workspace-setup-reviewer | 4054.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.4 ms |
| TS-081 | complete-workspace-suggestion-success | 0.5 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.5 ms |
| TS-121 | workspace-request-verifier | 342.5 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.3 ms |
| TS-121 | workspace-requirement-rephraser | 3616.8 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.1 ms |
| TS-121 | catalogue-product-retriever | 8343.4 ms | 2 | 4287.2 ms | 3662.3 ms | 387.6 ms | 6 | 6.3 ms |
| TS-121 | build-candidate-product-pool | 0.8 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.8 ms |
| TS-121 | catalogue-candidate-reranker | 13757.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.9 ms |
| TS-121 | workspace-setup-composer | 4642.2 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.3 ms |
| TS-121 | validate-workspace-setup-structure | 0.3 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.3 ms |
| TS-121 | workspace-setup-reviewer | 2125.1 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.4 ms |
| TS-121 | decide-workspace-setup-retry | 0.1 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.1 ms |
| TS-121 | workspace-requirement-rephraser | 3928.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 3.4 ms |
| TS-121 | catalogue-product-retriever | 8627.0 ms | 2 | 4560.7 ms | 3714.3 ms | 343.4 ms | 6 | 8.7 ms |
| TS-121 | build-candidate-product-pool | 0.5 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.5 ms |
| TS-121 | catalogue-candidate-reranker | 8054.6 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 7.3 ms |
| TS-161 | workspace-request-verifier | 963.3 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.9 ms |
| TS-161 | workspace-requirement-rephraser | 4501.3 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.1 ms |
| TS-161 | catalogue-product-retriever | 8435.8 ms | 2 | 4410.8 ms | 3834.2 ms | 184.2 ms | 6 | 6.7 ms |
| TS-161 | build-candidate-product-pool | 0.5 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.5 ms |
| TS-161 | catalogue-candidate-reranker | 10643.6 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.4 ms |
| TS-161 | workspace-setup-composer | 4157.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.8 ms |
| TS-161 | validate-workspace-setup-structure | 0.2 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.2 ms |
| TS-161 | workspace-setup-reviewer | 1060.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.2 ms |
| TS-161 | complete-workspace-suggestion-success | 10.2 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 10.2 ms |

## Metrics

| Metric | Case pass rate |
|---|---:|
| `answer_exact` | 60.0 % |
| `constraint_compliance` | 60.0 % |
| `raw_retrieval_recall` | 100.0 % |
| `reranker_aligned` | 100.0 % |
| `retrieval_quality` | 80.0 % |
| `tool_call_accuracy` | 100.0 % |
| `workflow_correctness` | 60.0 % |

## Stability

| Repetitions | Rows |
|---|---:|
| 1/1 | 3 |
| 0/1 | 2 |

## Judge

| Case | Pass | Confidence | Attempts | Reason |
|---|---|---:|---:|---|
| TS-001 | FAIL | 1.00 | 1 | The final setup does not include a mahogany desk or a green velvet armless chair as requested. |
| TS-121 | FAIL | 1.00 | 1 | The final setup contains no items, so it does not provide a black mobile desk, a mesh chair, or a Monstera plant as requested. All required slots are missing. |

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
| Recommendation Diversity | - | Computed | 1.000 | 9 | - |
| Category Exposure | - | Computed | 0.040 | 9 | - |
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
| Evidence Grounding Rate | builder | Computed | 1.000 | 24 | - |
| Tool Selection Disparity | retriever | Computed | 0.000 | 3 | - |
| Constraint Violation Rate | builder | Computed | 0.400 | 5 | - |
| Reviewer Disagreement Rate | reviewer | Computed | 0.400 | 5 | - |

### C.1 Stage breakdown

| Stage | Metric | Status | Value | n |
|---|---|---|---:|---:|
| verifier | Verifier Refusal Rate | Computed | 0.000 | 5 |
| rephraser | Unsupported Assumption Rate | Computed | 0.000 | 5 |
| builder | Evidence Grounding Rate | Computed | 1.000 | 24 |
| retriever | Tool Selection Disparity | Computed | 0.000 | 3 |
| builder | Constraint Violation Rate | Computed | 0.400 | 5 |
| reviewer | Reviewer Disagreement Rate | Computed | 0.400 | 5 |

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
| 1 | TS-001 | 0/1 | 0.00 | 1.00 | NO_CANDIDATE<br>WORKFLOW_STATUS:missing<br>WORKFLOW_COMPLETED_NOT_LAST<br>WORKFLOW_NO_CANDIDATE<br>WORKFLOW_SEQUENCE<br>CASE_TIMEOUT<br>JUDGE_REJECTED |
| 121 | TS-121 | 0/1 | 0.00 | 0.67 | NO_CANDIDATE<br>RETRIEVAL_MISS:CHAB09RLYPZ1<br>WORKFLOW_STATUS:missing<br>WORKFLOW_COMPLETED_NOT_LAST<br>WORKFLOW_NO_CANDIDATE<br>WORKFLOW_SEQUENCE<br>CASE_TIMEOUT<br>JUDGE_REJECTED |

## All rows

| Row | Case | Stability | Reranker aligned | Judge | Case pass | Failure codes |
|---:|---|---|---|---|---|---|
| 1 | TS-001 | 0/1 | YES | FAIL | FAIL | NO_CANDIDATE<br>WORKFLOW_STATUS:missing<br>WORKFLOW_COMPLETED_NOT_LAST<br>WORKFLOW_NO_CANDIDATE<br>WORKFLOW_SEQUENCE<br>CASE_TIMEOUT<br>JUDGE_REJECTED |
| 41 | TS-041 | 1/1 | YES | - | PASS |  |
| 81 | TS-081 | 1/1 | YES | - | PASS |  |
| 121 | TS-121 | 0/1 | YES | FAIL | FAIL | NO_CANDIDATE<br>RETRIEVAL_MISS:CHAB09RLYPZ1<br>WORKFLOW_STATUS:missing<br>WORKFLOW_COMPLETED_NOT_LAST<br>WORKFLOW_NO_CANDIDATE<br>WORKFLOW_SEQUENCE<br>CASE_TIMEOUT<br>JUDGE_REJECTED |
| 161 | TS-161 | 1/1 | YES | - | PASS |  |
