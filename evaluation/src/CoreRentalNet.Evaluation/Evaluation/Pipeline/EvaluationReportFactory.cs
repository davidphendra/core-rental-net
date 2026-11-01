using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;
using Microsoft.Agents.AI;
using CoreRentalNet.Evaluation.Reporting.Bias;
using Microsoft.Extensions.AI.Evaluation;
using WorkspaceContext = CoreRentalNet.Evaluation.Evidence.Context.EvaluationContext;

using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Pipeline;

/// <summary>Projects built-in evaluator results into the 200-row report.</summary>
public sealed class EvaluationReportFactory(
    IReadOnlyDictionary<string, ProductRecord>? catalogue = null,
    BiasReportBuilder? biasReportBuilder = null,
    bool judgeConfigured = false)
{
    public EvaluationReport Create(
        IReadOnlyList<RowRun> runs,
        IReadOnlyList<EvalItem> items,
        IReadOnlyList<AgentEvaluationResults> results,
        EvaluationScope scope,
        int totalRows,
        string? stopReason)
    {
        var metrics = ProjectMetrics(runs, results);
        var itemsByRunId = runs
            .Select((run, index) => (run.RunId, Item: items[index]))
            .ToDictionary(pair => pair.RunId, pair => pair.Item, StringComparer.Ordinal);

        var cases = runs
            .GroupBy(run => run.Case.RowNumber)
            .Select(group => CreateCase(group.ToArray(), metrics, itemsByRunId))
            .OrderBy(testCase => testCase.RowNumber)
            .ToArray();

        var bias = biasReportBuilder is null || catalogue is null
            ? null
            : biasReportBuilder.Build(new BiasInput(
                cases,
                runs.Select((run, index) => new BiasRun(run, WorkspaceContext.Parse(items[index].Context))).ToArray(),
                metrics,
                catalogue));

        return new EvaluationReport(
            Cases: cases,
            Metrics: metrics,
            Summary: Summarize(cases, metrics, runs.Count, judgeConfigured))
        {
            Scope = scope,
            TotalRows = totalRows,
            StopReason = stopReason,
            Bias = bias,
        };
    }

    private static IReadOnlyList<MetricRow> ProjectMetrics(
        IReadOnlyList<RowRun> runs,
        IReadOnlyList<AgentEvaluationResults> results)
    {
        var rows = new List<MetricRow>();

        foreach (var result in results)
        {
            if (result.Items.Count != runs.Count)
            {
                throw new InvalidOperationException(
                    $"{result.ProviderName} returned {result.Items.Count} items for {runs.Count} runs.");
            }

            for (var index = 0; index < result.Items.Count; index++)
            {
                var run = runs[index];

                foreach (var metric in result.Items[index].Metrics.Values)
                {
                    rows.Add(new MetricRow(
                        RunId: run.RunId,
                        RowNumber: run.Case.RowNumber,
                        Repetition: run.Repetition,
                        Provider: result.ProviderName,
                        Metric: metric.Name,
                        Passed: Passed(metric),
                        Value: ValueOf(metric),
                        Reason: metric.Reason)
                    {
                        Measured = IsMeasured(metric),
                    });
                }
            }
        }

        return rows;
    }

    private static CaseResult CreateCase(
        IReadOnlyList<RowRun> runs,
        IReadOnlyList<MetricRow> metrics,
        IReadOnlyDictionary<string, EvalItem> itemsByRunId)
    {
        var repetitions = runs
            .Select(run => CreateRepetition(run, metrics, itemsByRunId[run.RunId]))
            .OrderBy(repetition => repetition.Repetition)
            .ToArray();

        var passCount = repetitions.Count(repetition => repetition.CasePass);
        var casePass = repetitions.Length > 0
                       && repetitions.All(repetition => repetition.CasePass);

        return new CaseResult(
            RowNumber: runs[0].Case.RowNumber,
            CaseId: runs[0].Case.CaseId,
            Sentence: runs[0].Case.Sentence,
            Repetitions: repetitions,
            CasePass: casePass,
            Stability: $"{passCount}/{repetitions.Length}",
            FailureCodes: [.. repetitions.SelectMany(repetition => repetition.FailureCodes).Distinct(StringComparer.Ordinal)],
            MeanAnswerF1: repetitions.Average(repetition => repetition.AnswerF1),
            MeanRetrievalRecall: repetitions.Average(repetition => repetition.RetrievalRecall))
        {
            ActualOutput = ActualOutput(runs[0], itemsByRunId),
        };
    }

    private static string ActualOutput(
        RowRun run,
        IReadOnlyDictionary<string, EvalItem> itemsByRunId)
    {
        var context = WorkspaceContext.Parse(itemsByRunId[run.RunId].Context);

        return EvaluationText.Candidate(context.Candidates);
    }

    private static RepetitionResult CreateRepetition(
        RowRun run,
        IReadOnlyList<MetricRow> metrics,
        EvalItem item)
    {
        var local = metrics
            .Where(metric => metric.RunId == run.RunId && MetricNames.All.Contains(metric.Metric))
            .ToDictionary(metric => metric.Metric, metric => metric.Passed, StringComparer.Ordinal);

        var judge = metrics
            .Where(metric => metric.RunId == run.RunId && MetricNames.JudgeMetrics.Contains(metric.Metric))
            .ToDictionary(metric => metric.Metric, metric => metric, StringComparer.Ordinal);
        var judgeEvaluated = judge.TryGetValue(MetricNames.JudgeEvaluated, out var evaluatedRow)
            ? evaluatedRow.Passed
            : (bool?)null;
        var judgePass = judge.TryGetValue(MetricNames.JudgePass, out var passRow)
            ? passRow.Passed
            : (bool?)null;
        var judgeConfidence = judge.TryGetValue(MetricNames.JudgeConfidence, out var confidenceRow)
            ? confidenceRow.Value
            : null;
        var judgeAttempts = judge.TryGetValue(MetricNames.JudgeAttempts, out var attemptsRow)
            ? (int?)attemptsRow.Value
            : null;
        var judgeReason = passRow?.Reason;

        string[] failures = run.TimedOut
            ? [FailureCodes.CaseTimeout]
            : [];

        var context = WorkspaceContext.Parse(item.Context);
        var answer = AnswerComparison.Evaluate(context);
        var retrieval = RetrievalQuality.Evaluate(context);
        var casePass = run.Completed && !run.TimedOut && context.Candidates.Count > 0;

        return new RepetitionResult(
            Repetition: run.Repetition,
            Metrics: local,
            FailureCodes: failures,
            AnswerF1: answer.F1,
            RetrievalRecall: retrieval.SelectedRecall)
        {
            Timing = RunTimingFactory.Create(run.Trace, run.WallMs),
            JudgeEvaluated = judgeEvaluated,
            JudgePass = judgePass,
            JudgeConfidence = judgeConfidence,
            JudgeAttempts = judgeAttempts,
            JudgeReason = judgeReason,
            CasePass = casePass,
        };
    }

    private static EvaluationSummary Summarize(
        IReadOnlyList<CaseResult> cases,
        IReadOnlyList<MetricRow> metrics,
        int executions,
        bool judgeConfigured)
    {
        var metricPassRates = MetricNames.All.ToDictionary(
            metric => metric,
            metric => cases.Count(testCase =>
                testCase.Repetitions.Count(repetition =>
                    repetition.Metrics.TryGetValue(metric, out var passed) && passed)
                >= Math.Min(2, testCase.Repetitions.Count))
                / (double)Math.Max(cases.Count, 1),
            StringComparer.Ordinal);

        var stability = cases
            .GroupBy(testCase => testCase.Stability, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var passed = cases.Count(testCase => testCase.CasePass);
        var judged = cases.SelectMany(testCase => testCase.Repetitions)
            .Count(repetition => repetition.JudgeEvaluated == true);
        var judgePass = cases.SelectMany(testCase => testCase.Repetitions)
            .Count(repetition => repetition.JudgePass == true);

        var ragRows = metrics
            .Where(row => MetricNames.RagMetrics.Contains(row.Metric))
            .ToArray();

        var ragPassRates = MetricNames.RagMetrics.ToDictionary(
            metric => metric,
            metric =>
            {
                var measured = ragRows
                    .Where(row => string.Equals(row.Metric, metric, StringComparison.Ordinal) && row.Measured)
                    .ToArray();

                return measured.Length == 0
                    ? (double?)null
                    : measured.Count(row => row.Passed) / (double)measured.Length;
            },
            StringComparer.Ordinal);

        var ragMeans = MetricNames.RagMetrics.ToDictionary(
            metric => metric,
            metric =>
            {
                var measured = ragRows
                    .Where(row => string.Equals(row.Metric, metric, StringComparison.Ordinal)
                                  && row.Measured
                                  && row.Value is not null)
                    .ToArray();

                return measured.Length == 0
                    ? (double?)null
                    : measured.Average(row => row.Value!.Value);
            },
            StringComparer.Ordinal);

        return new EvaluationSummary(
            Rows: cases.Count,
            Executions: executions,
            CasePass: passed,
            CasePassRate: cases.Count == 0 ? 0 : passed / (double)cases.Count,
            MetricPassRates: metricPassRates,
            Stability: stability)
        {
            JudgeConfigured = judgeConfigured,
            JudgeEvaluated = judged,
            JudgePass = judgePass,
            JudgePassRate = judged == 0 ? null : judgePass / (double)judged,
            RagMetricPassRates = ragPassRates,
            RagMetricMeans = ragMeans,
            RagMeasured = ragRows.Count(row => row.Measured),
            RagNotMeasured = ragRows.Count(row => !row.Measured),
        };
    }

    private static bool Passed(EvaluationMetric metric)
        => metric switch
        {
            BooleanMetric boolean => boolean.Value == true,
            _ => metric.Interpretation?.Failed == false,
        };

    private static bool IsMeasured(EvaluationMetric metric)
        => metric switch
        {
            BooleanMetric boolean => boolean.Value is not null,
            NumericMetric numeric => numeric.Value is not null,
            _ => true,
        };

    private static double? ValueOf(EvaluationMetric metric)
        => metric switch
        {
            BooleanMetric boolean => boolean.Value is { } value ? (value ? 1 : 0) : null,
            NumericMetric numeric => numeric.Value is { } value ? Convert.ToDouble(value) : null,
            _ => null,
        };
}
