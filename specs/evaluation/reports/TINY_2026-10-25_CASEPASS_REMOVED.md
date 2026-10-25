# Workspace Suggestion Evaluation

Scope: **Tiny** (5 of 200 rows)  
Rows: **5**  
Executions: **5**  
Case pass: **2/5 (40.0 %)**  
Judge: **configured; no eligible cases**

## Timing

| Case | Wall | Node total | Unaccounted | Model | Tool | Guardrail | Host | Slowest stage |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| TS-001 | 180118.5 ms | 44395.5 ms | 135723.0 ms | 43805.3 ms | 391.6 ms | 2.4 ms | 5.1 ms | catalogue-candidate-reranker |
| TS-041 | 180108.6 ms | 108061.9 ms | 72046.8 ms | 107863.4 ms | 180.1 ms | 0.4 ms | 1.9 ms | catalogue-candidate-reranker |
| TS-081 | 142452.4 ms | 34203.6 ms | 108248.9 ms | 33889.7 ms | 265.9 ms | 0.5 ms | 5.7 ms | catalogue-candidate-reranker |
| TS-121 | 180113.3 ms | 53683.2 ms | 126430.1 ms | 52670.8 ms | 966.8 ms | 0.9 ms | 13.2 ms | catalogue-candidate-reranker |
| TS-161 | 130144.6 ms | 30245.7 ms | 99898.9 ms | 29891.1 ms | 327.5 ms | 0.7 ms | 4.5 ms | catalogue-candidate-reranker |

### Stage timing

| Case | Stage | Total | Model calls | Tool planning | Final structured | Tool exec | Tool calls | Unattributed |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| TS-001 | workspace-request-verifier | 2698.6 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 34.8 ms |
| TS-001 | workspace-requirement-rephraser | 3034.3 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 30.8 ms |
| TS-001 | catalogue-product-retriever | 5983.5 ms | 2 | 3435.8 ms | 2260.5 ms | 204.8 ms | 4 | 82.4 ms |
| TS-001 | build-candidate-product-pool | 7.2 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 7.2 ms |
| TS-001 | catalogue-candidate-reranker | 8010.1 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 8.2 ms |
| TS-001 | workspace-setup-composer | 3872.1 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 5.1 ms |
| TS-001 | validate-workspace-setup-structure | 5.7 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 5.7 ms |
| TS-001 | workspace-setup-reviewer | 3872.9 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 9.1 ms |
| TS-001 | decide-workspace-setup-retry | 2.5 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.5 ms |
| TS-001 | workspace-requirement-rephraser | 4167.0 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 3.1 ms |
| TS-001 | catalogue-product-retriever | 5702.7 ms | 2 | 3001.9 ms | 2506.2 ms | 186.8 ms | 4 | 7.8 ms |
| TS-001 | build-candidate-product-pool | 0.4 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.4 ms |
| TS-001 | catalogue-candidate-reranker | 7038.3 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.4 ms |
| TS-041 | workspace-request-verifier | 377.8 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.3 ms |
| TS-041 | workspace-requirement-rephraser | 3838.2 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.0 ms |
| TS-041 | catalogue-product-retriever | 8721.5 ms | 2 | 4392.7 ms | 4139.9 ms | 180.1 ms | 6 | 8.9 ms |
| TS-041 | build-candidate-product-pool | 0.9 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.9 ms |
| TS-041 | catalogue-candidate-reranker | 91159.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.6 ms |
| TS-041 | workspace-setup-composer | 3963.4 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.5 ms |
| TS-041 | validate-workspace-setup-structure | 0.3 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.3 ms |
| TS-081 | workspace-request-verifier | 408.0 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.2 ms |
| TS-081 | workspace-requirement-rephraser | 3426.8 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 9.1 ms |
| TS-081 | catalogue-product-retriever | 9132.2 ms | 2 | 4426.5 ms | 4421.2 ms | 265.9 ms | 6 | 18.7 ms |
| TS-081 | build-candidate-product-pool | 0.5 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.5 ms |
| TS-081 | catalogue-candidate-reranker | 12630.1 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.5 ms |
| TS-081 | workspace-setup-composer | 4279.8 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.8 ms |
| TS-081 | validate-workspace-setup-structure | 0.9 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.9 ms |
| TS-081 | workspace-setup-reviewer | 4314.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.8 ms |
| TS-081 | complete-workspace-suggestion-success | 10.5 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 10.5 ms |
| TS-121 | workspace-request-verifier | 453.1 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.3 ms |
| TS-121 | workspace-requirement-rephraser | 3650.3 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 2.0 ms |
| TS-121 | catalogue-product-retriever | 9133.6 ms | 2 | 4686.6 ms | 3981.7 ms | 452.1 ms | 6 | 13.2 ms |
| TS-121 | build-candidate-product-pool | 0.5 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.5 ms |
| TS-121 | catalogue-candidate-reranker | 13974.0 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.8 ms |
| TS-121 | workspace-setup-composer | 4477.0 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.7 ms |
| TS-121 | validate-workspace-setup-structure | 0.2 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.2 ms |
| TS-121 | workspace-setup-reviewer | 2133.3 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.6 ms |
| TS-121 | decide-workspace-setup-retry | 0.1 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.1 ms |
| TS-121 | workspace-requirement-rephraser | 3665.5 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.8 ms |
| TS-121 | catalogue-product-retriever | 8309.4 ms | 2 | 4115.0 ms | 3665.8 ms | 514.7 ms | 6 | 13.9 ms |
| TS-121 | build-candidate-product-pool | 0.9 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.9 ms |
| TS-121 | catalogue-candidate-reranker | 7885.3 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 6.6 ms |
| TS-161 | workspace-request-verifier | 857.3 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.9 ms |
| TS-161 | workspace-requirement-rephraser | 4508.1 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 3.1 ms |
| TS-161 | catalogue-product-retriever | 9061.0 ms | 2 | 4499.1 ms | 4217.8 ms | 327.5 ms | 6 | 16.6 ms |
| TS-161 | build-candidate-product-pool | 0.3 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.3 ms |
| TS-161 | catalogue-candidate-reranker | 10673.0 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.7 ms |
| TS-161 | workspace-setup-composer | 4125.2 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.5 ms |
| TS-161 | validate-workspace-setup-structure | 0.2 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.2 ms |
| TS-161 | workspace-setup-reviewer | 1019.7 ms | 1 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 1.0 ms |
| TS-161 | complete-workspace-suggestion-success | 0.7 ms | 0 | 0.0 ms | 0.0 ms | 0.0 ms | 0 | 0.7 ms |

## Metrics

| Metric | Case pass rate |
|---|---:|
| `answer_exact` | 40.0 % |
| `constraint_compliance` | 40.0 % |
| `raw_retrieval_recall` | 40.0 % |
| `reranker_aligned` | 40.0 % |
| `retrieval_quality` | 40.0 % |
| `tool_call_accuracy` | 40.0 % |
| `workflow_correctness` | 40.0 % |

## Stability

| Repetitions | Rows |
|---|---:|
| 1/1 | 2 |
| 0/1 | 3 |

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
| Recommendation Diversity | - | Computed | 1.000 | 6 | - |
| Category Exposure | - | Computed | 0.095 | 6 | - |
| Retrieval Coverage | - | Computed | 0.857 | 14 | - |
| Counterfactual Consistency | - | NotApplicable | - | 0 | No counterfactual pairs exist in the fixture. |
| Attribute Sensitivity | - | NotApplicable | - | 0 | No controlled attribute pairs exist in the fixture. |

## C. Agent reasoning and decision bias

| Metric | Stage | Status | Value | n | Reason |
|---|---|---|---:|---:|---|
| Verifier Refusal Rate | verifier | Computed | 0.000 | 5 | - |
| Decision Consistency Rate | - | InsufficientData | - | 0 | No duplicate sentence groups exist in the selected scope. |
| Counterfactual Flip Rate | - | NotApplicable | - | 0 | No counterfactual pairs exist in the fixture. |
| Unsupported Assumption Rate | rephraser | Computed | 0.000 | 5 | - |
| Evidence Grounding Rate | builder | Computed | 1.000 | 6 | - |
| Tool Selection Disparity | retriever | Computed | 2.000 | 3 | - |
| Constraint Violation Rate | builder | Computed | 0.600 | 5 | - |
| Reviewer Disagreement Rate | reviewer | Computed | 0.250 | 4 | - |

### C.1 Stage breakdown

| Stage | Metric | Status | Value | n |
|---|---|---|---:|---:|
| verifier | Verifier Refusal Rate | Computed | 0.000 | 5 |
| rephraser | Unsupported Assumption Rate | Computed | 0.000 | 5 |
| builder | Evidence Grounding Rate | Computed | 1.000 | 6 |
| retriever | Tool Selection Disparity | Computed | 2.000 | 3 |
| builder | Constraint Violation Rate | Computed | 0.600 | 5 |
| reviewer | Reviewer Disagreement Rate | Computed | 0.250 | 4 |

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
| 1 | TS-001 | 0/1 | 0.00 | 0.00 | NO_CANDIDATE<br>RETRIEVAL_MISS:DSKB08XN4JDR<br>RETRIEVAL_MISS:CHAB09R9WB61<br>TOOL_NOT_CALLED:Desk<br>TOOL_NOT_CALLED:Chair<br>WORKFLOW_STATUS:missing<br>WORKFLOW_COMPLETED_NOT_LAST<br>WORKFLOW_NO_CANDIDATE<br>WORKFLOW_SEQUENCE<br>CASE_TIMEOUT |
| 41 | TS-041 | 0/1 | 0.00 | 0.00 | NO_CANDIDATE<br>RETRIEVAL_MISS:DSKB0B6MCK87<br>RETRIEVAL_MISS:CHAB0776MKCL<br>RETRIEVAL_MISS:PLTB015HHXT9<br>TOOL_NOT_CALLED:Desk<br>TOOL_NOT_CALLED:Chair<br>TOOL_NOT_CALLED:Plant<br>WORKFLOW_STATUS:missing<br>WORKFLOW_COMPLETED_NOT_LAST<br>WORKFLOW_NO_CANDIDATE<br>WORKFLOW_SEQUENCE<br>CASE_TIMEOUT |
| 121 | TS-121 | 0/1 | 0.00 | 0.00 | NO_CANDIDATE<br>RETRIEVAL_MISS:DSKB07Z74NMV<br>RETRIEVAL_MISS:CHAB09RLYPZ1<br>RETRIEVAL_MISS:PLTB0BJDQ4R5<br>TOOL_NOT_CALLED:Desk<br>TOOL_NOT_CALLED:Chair<br>TOOL_NOT_CALLED:Plant<br>WORKFLOW_STATUS:missing<br>WORKFLOW_COMPLETED_NOT_LAST<br>WORKFLOW_NO_CANDIDATE<br>WORKFLOW_SEQUENCE<br>CASE_TIMEOUT |

## All rows

| Row | Case | Stability | Reranker aligned | Judge | Case pass | Failure codes |
|---:|---|---|---|---|---|---|
| 1 | TS-001 | 0/1 | NO | - | FAIL | NO_CANDIDATE<br>RETRIEVAL_MISS:DSKB08XN4JDR<br>RETRIEVAL_MISS:CHAB09R9WB61<br>TOOL_NOT_CALLED:Desk<br>TOOL_NOT_CALLED:Chair<br>WORKFLOW_STATUS:missing<br>WORKFLOW_COMPLETED_NOT_LAST<br>WORKFLOW_NO_CANDIDATE<br>WORKFLOW_SEQUENCE<br>CASE_TIMEOUT |
| 41 | TS-041 | 0/1 | NO | - | FAIL | NO_CANDIDATE<br>RETRIEVAL_MISS:DSKB0B6MCK87<br>RETRIEVAL_MISS:CHAB0776MKCL<br>RETRIEVAL_MISS:PLTB015HHXT9<br>TOOL_NOT_CALLED:Desk<br>TOOL_NOT_CALLED:Chair<br>TOOL_NOT_CALLED:Plant<br>WORKFLOW_STATUS:missing<br>WORKFLOW_COMPLETED_NOT_LAST<br>WORKFLOW_NO_CANDIDATE<br>WORKFLOW_SEQUENCE<br>CASE_TIMEOUT |
| 81 | TS-081 | 1/1 | YES | - | PASS |  |
| 121 | TS-121 | 0/1 | NO | - | FAIL | NO_CANDIDATE<br>RETRIEVAL_MISS:DSKB07Z74NMV<br>RETRIEVAL_MISS:CHAB09RLYPZ1<br>RETRIEVAL_MISS:PLTB0BJDQ4R5<br>TOOL_NOT_CALLED:Desk<br>TOOL_NOT_CALLED:Chair<br>TOOL_NOT_CALLED:Plant<br>WORKFLOW_STATUS:missing<br>WORKFLOW_COMPLETED_NOT_LAST<br>WORKFLOW_NO_CANDIDATE<br>WORKFLOW_SEQUENCE<br>CASE_TIMEOUT |
| 161 | TS-161 | 1/1 | YES | - | PASS |  |
