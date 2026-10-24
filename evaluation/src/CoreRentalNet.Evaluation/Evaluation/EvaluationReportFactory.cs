using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evidence;
using CoreRentalNet.Evaluation.Execution;
using Microsoft.Agents.AI;
using CoreRentalNet.Evaluation.Reporting.Bias;
using Microsoft.Extensions.AI.Evaluation;
using WorkspaceContext = CoreRentalNet.Evaluation.Evidence.EvaluationContext;

namespace CoreRentalNet.Evaluation.Evaluation;

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
            Summary: Summarize(cases, runs.Count, judgeConfigured))
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
                        Reason: metric.Reason));
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
        var casePass = CasePassPolicy.Passed(passCount, repetitions.Length);

        return new CaseResult(
            RowNumber: runs[0].Case.RowNumber,
            CaseId: runs[0].Case.CaseId,
            Sentence: runs[0].Case.Sentence,
            Repetitions: repetitions,
            CasePass: casePass,
            Stability: $"{passCount}/{repetitions.Length}",
            FailureCodes: [.. repetitions.SelectMany(repetition => repetition.FailureCodes).Distinct(StringComparer.Ordinal)],
            MeanAnswerF1: repetitions.Average(repetition => repetition.AnswerF1),
            MeanRetrievalRecall: repetitions.Average(repetition => repetition.RetrievalRecall));
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
        var judgeReason = passRow?.Reason;

        var failures = metrics
            .Where(metric => metric.RunId == run.RunId
                             && MetricNames.CasePassMetrics.Contains(metric.Metric)
                             && !metric.Passed)
            .SelectMany(metric => (metric.Reason ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(code => code.Length > 0)
            .Concat(run.TimedOut ? [FailureCodes.CaseTimeout] : [])
            .Concat(judgeEvaluated == true && judgePass != true
                ? [JudgeFailureCode(judgeReason)]
                : [])
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var context = WorkspaceContext.Parse(item.Context);
        var answer = AnswerComparison.Evaluate(context);
        var retrieval = RetrievalQuality.Evaluate(context);
        var alignment = RerankerAlignment.Evaluate(context);
        var casePass = CasePassPolicy.Passed(
            local,
            alignment.Aligned,
            run.TimedOut,
            judgeEvaluated == true,
            judgePass == true);

        return new RepetitionResult(
            Repetition: run.Repetition,
            Metrics: local,
            FailureCodes: casePass ? [] : failures,
            AnswerF1: answer.F1,
            RetrievalRecall: retrieval.SelectedRecall)
        {
            Timing = RunTimingFactory.Create(run.Trace, run.WallMs),
            RerankerAligned = alignment.Aligned,
            JudgeEvaluated = judgeEvaluated,
            JudgePass = judgePass,
            JudgeConfidence = judgeConfidence,
            JudgeReason = judgeReason,
            CasePass = casePass,
        };
    }

    private static EvaluationSummary Summarize(
        IReadOnlyList<CaseResult> cases,
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
        };
    }

    private static string JudgeFailureCode(string? reason)
        => reason?.StartsWith("JUDGE_ERROR", StringComparison.Ordinal) == true
            ? FailureCodes.JudgeError
            : FailureCodes.JudgeRejected;

    private static bool Passed(EvaluationMetric metric)
        => metric switch
        {
            BooleanMetric boolean => boolean.Value == true,
            _ => metric.Interpretation?.Failed == false,
        };

    private static double? ValueOf(EvaluationMetric metric)
        => metric switch
        {
            BooleanMetric boolean => boolean.Value == true ? 1 : 0,
            NumericMetric numeric => numeric.Value is { } value ? Convert.ToDouble(value) : null,
            _ => null,
        };
}
