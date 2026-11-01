# Metrics matrix

What each required metric is computed by, what data it needs, and whether it can be evaluated today.

Legend: ✅ evaluable · ⚠️ implemented but limited · ❌ not evaluable (annotation missing)

| # | Metric | Provider | Data required | Today | Limitation |
|---|---|---|---|---|---|
| 1 | relevance | `Microsoft.Extensions.AI.Evaluation.Quality.RelevanceEvaluator` | query + answer | ⚠️ | implemented; judge not yet exercised live |
| 2 | groundedness | AgentEval `FaithfulnessMetric` (old pipeline) | retrieved context | ✅ | needs `--import-agent-eval <report.json>` from a legacy `run` |
| 3 | completeness | `CompletenessEvaluator` + case facts | `expectedFacts` | ⚠️ | facts are **derived** from quantities unless annotated |
| 4 | answer correctness | deterministic + AgentEval | expected outcome, SKU set, equipment | ✅ | LLM equivalence half not wired |
| 5 | context precision | AgentEval `ContextPrecisionMetric` | retrieved context | ✅ | same as 2 |
| 6 | context recall | AgentEval `ContextRecallMetric` | relevant sources | ✅ | same as 2 |
| 7 | Recall@K (3/5/10) | local, deterministic | `relevantWorkspaceIds` | ⚠️ | **development only** — validation/test carry no labels |
| 8 | Precision@K (3/5/10) | local, deterministic | `relevantWorkspaceIds` | ⚠️ | same as 7; fixed-K denominator |
| 9 | nDCG@K (3/5/10) | local, deterministic | graded relevance | ⚠️ | same as 7; only grade 1 exists |
| 10 | stage comparison | local, deterministic | per-tool trace answers | ✅ | — |
| 11 | constraint compliance | local, deterministic | recommendation lines | ✅ | per-rule breakdown included |
| 12 | outcome accuracy | local, deterministic | expected outcome | ✅ | `indeterminate` excluded, not coerced |
| 13 | abstention quality | local, deterministic | expected outcome | ✅ | FAR + FRR, separate denominators |
| 14 | paraphrase consistency | local, deterministic | `paraphraseGroupId` | ❌ | no groups authored |
| 15 | noise sensitivity | local, deterministic | `noiseVariantGroupId` | ❌ | no variants authored |
| 16 | generalization gap | local, deterministic | two run records | ⚠️ | calculator ready; needs a dev run and a test run |
| 17 | category performance | local, deterministic | run record | ✅ | all metrics the record supports |
| 18 | confidence intervals | AgentEval `IStatisticsCalculator` | per-case values | ✅ | Wilson-style interval from the pinned calculator; sample size reported |

## Rules the numbers obey

- A metric with no labels returns `null`, never `0`.
- A metric whose sample is below 30 is marked **insufficient sample** and cannot pass.
- No threshold is configured in code; `evaluation/appsettings.json` sets none.
- A run with no recognised `WORKFLOW_STATUS` is `indeterminate` and excluded from accuracy.
