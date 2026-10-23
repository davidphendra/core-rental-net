using System.Globalization;
using System.Text;
using CoreRentalNet.Evaluation.Evaluation;

namespace CoreRentalNet.Evaluation.Reporting;

/// <summary>Writes a human report with failures first.</summary>
public static class MarkdownReportWriter
{
    public static async Task WriteAsync(
        EvaluationReport report,
        string path,
        CancellationToken cancellationToken)
    {
        var text = new StringBuilder();

        text.AppendLine("# Workspace Suggestion Evaluation");
        text.AppendLine();
        text.AppendLine($"Scope: **{report.Scope}** ({report.Summary.Rows} of {report.TotalRows} rows)  ");
        if (!string.IsNullOrWhiteSpace(report.StopReason))
        {
            text.AppendLine($"Stopped: **{report.StopReason}**  ");
        }

        text.AppendLine($"Rows: **{report.Summary.Rows}**  ");
        text.AppendLine($"Executions: **{report.Summary.Executions}**  ");
        text.AppendLine($"Case pass: **{report.Summary.CasePass}/{report.Summary.Rows} " +
                        $"({Percentage(report.Summary.CasePassRate)})**");
        text.AppendLine();
        AppendTiming(text, report);

        text.AppendLine();
        text.AppendLine("## Metrics");
        text.AppendLine();
        text.AppendLine("| Metric | Case pass rate |");
        text.AppendLine("|---|---:|");
        foreach (var (metric, rate) in report.Summary.MetricPassRates.OrderBy(pair => pair.Key))
        {
            text.AppendLine($"| `{metric}` | {Percentage(rate)} |");
        }

        text.AppendLine();
        text.AppendLine("## Stability");
        text.AppendLine();
        text.AppendLine("| Repetitions | Rows |");
        text.AppendLine("|---|---:|");
        foreach (var (stability, count) in report.Summary.Stability.OrderByDescending(pair => pair.Key))
        {
            text.AppendLine($"| {stability} | {count} |");
        }

        text.AppendLine();
        text.AppendLine("## Failing rows");
        text.AppendLine();
        text.AppendLine("| Row | Case | Stability | Answer F1 | Retrieval recall | Failure codes |");
        text.AppendLine("|---:|---|---|---:|---:|---|");

        foreach (var testCase in report.Cases.Where(testCase => !testCase.CasePass))
        {
            text.AppendLine(
                $"| {testCase.RowNumber} | {testCase.CaseId} | {testCase.Stability} | " +
                $"{testCase.MeanAnswerF1.ToString("0.00", CultureInfo.InvariantCulture)} | " +
                $"{testCase.MeanRetrievalRecall.ToString("0.00", CultureInfo.InvariantCulture)} | " +
                $"{string.Join("<br>", testCase.FailureCodes)} |");
        }

        text.AppendLine();
        text.AppendLine("## All rows");
        text.AppendLine();
        text.AppendLine("| Row | Case | Stability | Reranker aligned | Case pass | Failure codes |");
        text.AppendLine("|---:|---|---|---|---|---|");

        foreach (var testCase in report.Cases)
        {
            var aligned = testCase.Repetitions.Any(repetition => repetition.RerankerAligned);
            text.AppendLine(
                $"| {testCase.RowNumber} | {testCase.CaseId} | {testCase.Stability} | " +
                $"{(aligned ? "YES" : "NO")} | " +
                $"{(testCase.CasePass ? "PASS" : "FAIL")} | " +
                $"{string.Join("<br>", testCase.FailureCodes)} |");
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, text.ToString(), cancellationToken);
    }

    private static void AppendTiming(StringBuilder text, EvaluationReport report)
    {
        text.AppendLine("## Timing");
        text.AppendLine();
        text.AppendLine("| Case | Wall | Node total | Unaccounted | Model | Tool | Guardrail | Host | Slowest stage |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---|");

        foreach (var testCase in report.Cases)
        {
            foreach (var repetition in testCase.Repetitions)
            {
                if (repetition.Timing is not { } timing)
                {
                    continue;
                }

                text.AppendLine(
                    $"| {testCase.CaseId} | {Ms(timing.WallMs)} | {Ms(timing.TotalMs)} | " +
                    $"{Ms(timing.UnaccountedMs)} | {Ms(timing.ModelMs)} | {Ms(timing.ToolMs)} | " +
                    $"{Ms(timing.GuardrailMs)} | {Ms(timing.HostMs)} | {timing.SlowestStage ?? "-"} |");
            }
        }

        text.AppendLine();
        text.AppendLine("### Stage timing");
        text.AppendLine();
        text.AppendLine("| Case | Stage | Total | Model calls | Tool planning | Final structured | Tool exec | Tool calls | Unattributed |");
        text.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var testCase in report.Cases)
        {
            foreach (var repetition in testCase.Repetitions)
            {
                if (repetition.Timing is not { } timing)
                {
                    continue;
                }

                foreach (var stage in timing.Stages)
                {
                    text.AppendLine(
                        $"| {testCase.CaseId} | {stage.Stage} | {Ms(stage.TotalMs)} | " +
                        $"{stage.ModelCallCount} | {Ms(stage.ToolPlanningModelMs)} | " +
                        $"{Ms(stage.FinalStructuredModelMs)} | {Ms(stage.ToolMs)} | " +
                        $"{stage.ToolCallCount} | {Ms(stage.UnattributedMs)} |");
                }
            }
        }
    }

    private static string Ms(double value)
        => $"{value.ToString("0.0", CultureInfo.InvariantCulture)} ms";

    private static string Percentage(double value)
        => value.ToString("P1", CultureInfo.InvariantCulture);
}
