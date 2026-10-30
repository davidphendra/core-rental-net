# AgentEval adoption

The evaluation harness uses **AgentEval `1.0.0-rc.1`** for generic RAG and agent-quality
measurement. The package is pinned exactly in
`evaluation/src/CoreRentalNet.Evaluation/CoreRentalNet.Evaluation.csproj`.

AgentEval requires MAF `1.24.0`; the evaluation solution therefore pins
`Microsoft.Agents.AI` and `Microsoft.Agents.AI.OpenAI` to `1.24.0` and `OpenAI` to `2.14.0`.
The main application solution is unchanged. `dotnet list package --vulnerable` reports no
vulnerable packages for the evaluation project.

## Metric map

| Report metric | AgentEval metric | Gate |
|---|---|---|
| `llm_faithfulness` | `FaithfulnessMetric` | Diagnostic until calibrated |
| `llm_answer_correctness` | `AnswerCorrectnessMetric` | Diagnostic until calibrated |
| `llm_context_precision` | `ContextPrecisionMetric` | Diagnostic until calibrated |
| `llm_context_recall` | `ContextRecallMetric` | Diagnostic until calibrated |
| `agenteval_tool_chain` | `ToolUsageReport.Should()` | Deterministic |
| `agenteval_workflow_nodes` | `WorkflowExecutionResult.Should()` | Deterministic |
| `code_tool_success` | `ToolSuccessMetric` | Not adopted yet |

The domain `answer_exact`, `constraint_compliance`, retrieval, tool-accuracy and workflow checks
remain local because AgentEval cannot express their slot/SKU semantics. The alternate-product
equivalence judge remains local for the same reason.

## Inputs

- `Context` is the **retrieved catalogue evidence only**: final tool answers, returned products,
  rerank selections and their names/descriptions/prices. Expected variants and capacities never
  enter it.
- `GroundTruth` is the best-matching expected setup, chosen by the existing deterministic F1. The
  chosen variant must be recorded when the projection is debugged.
- `Input` is the user sentence; `Output` is the final approved workspace setup.

## Stochastic, regression and reporting

- Stochastic statistics use AgentEval's `DefaultStatisticsCalculator` and
  `StochasticResult.DecideVerdict` over the repetitions the trace-aware runner already collected.
- Regression uses AgentEval's `RunComparison.Of` with `StimulusHash` and `ComparabilityFacts`.
  A report without a policy digest compares as `legacy`; a run produced under the current policy
  is therefore incomparable with a legacy report, which is deliberate.
- Reports are rendered through AgentEval's `MarkdownExporter`; `Print()` is a thin local wrapper
  because `AgentEval.Models.EvaluationReport` has no `Print()` method in `1.0.0-rc.1`.

## Upgrade policy

- Re-evaluate on each `1.0.0-rc.x`.
- Move to stable `1.0.0` when it ships, then record the new version here.
- Re-run the offline tests and one tiny live run before accepting an upgrade.

## Licence note

AgentEval is MIT. It pulls QuestPDF transitively for PDF rendering. The harness does not call
AgentEval's PDF renderers; QuestPDF's transitive-dependency terms are the recorded position.
