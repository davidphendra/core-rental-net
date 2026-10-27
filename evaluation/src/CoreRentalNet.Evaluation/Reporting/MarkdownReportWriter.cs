using System.Globalization;
using System.Text;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Reporting.Bias;

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
                        $"({Percentage(report.Summary.CasePassRate)})**  ");
        text.AppendLine($"Judge: **{JudgeSummary(report.Summary)}**");
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

        AppendJudge(text, report);
        AppendBias(text, report.Bias);

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
        text.AppendLine("| Row | Case | Stability | Judge | Case pass | Failure codes |");
        text.AppendLine("|---:|---|---|---|---|---|");

        foreach (var testCase in report.Cases)
        {
            text.AppendLine(
                $"| {testCase.RowNumber} | {testCase.CaseId} | {testCase.Stability} | " +
                $"{JudgeCell(testCase)} | " +
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

    private static void AppendJudge(StringBuilder text, EvaluationReport report)
    {
        var judged = report.Cases
            .SelectMany(testCase => testCase.Repetitions)
            .Where(repetition => repetition.JudgeEvaluated == true)
            .ToArray();

        if (judged.Length == 0)
        {
            return;
        }

        text.AppendLine();
        text.AppendLine("## Judge");
        text.AppendLine();
        text.AppendLine("| Case | Pass | Confidence | Attempts | Reason |");
        text.AppendLine("|---|---|---:|---:|---|");

        foreach (var testCase in report.Cases)
        {
            foreach (var repetition in testCase.Repetitions.Where(repetition => repetition.JudgeEvaluated == true))
            {
                text.AppendLine(
                    $"| {testCase.CaseId} | {(repetition.JudgePass == true ? "PASS" : "FAIL")} | " +
                    $"{repetition.JudgeConfidence?.ToString("0.00", CultureInfo.InvariantCulture) ?? "-"} | " +
                    $"{repetition.JudgeAttempts?.ToString(CultureInfo.InvariantCulture) ?? "-"} | " +
                    $"{Escape(repetition.JudgeReason)} |");
            }
        }
    }

    private static void AppendBias(StringBuilder text, BiasReport? bias)
    {
        if (bias is null)
        {
            return;
        }

        foreach (var section in Enum.GetValues<BiasSectionName>())
        {
            text.AppendLine();
            text.AppendLine($"## {Title(section)}");
            text.AppendLine();
            text.AppendLine("| Metric | Stage | Status | Value | n | Reason |");
            text.AppendLine("|---|---|---|---:|---:|---|");

            foreach (var metric in bias.Metrics.Where(metric => metric.Section == section))
            {
                text.AppendLine(
                    $"| {metric.Name} | {metric.Stage ?? "-"} | {metric.Status} | " +
                    $"{(metric.Value is { } value ? value.ToString("0.000", CultureInfo.InvariantCulture) : "-")} | " +
                    $"{metric.SampleSize} | {Escape(metric.Reason)} |");
            }

            if (section == BiasSectionName.Reasoning)
            {
                AppendStageBreakdown(text, bias);
            }
        }
    }

    private static void AppendStageBreakdown(StringBuilder text, BiasReport bias)
    {
        var staged = bias.Metrics
            .Where(metric => metric.Section == BiasSectionName.Reasoning && metric.Stage is not null)
            .ToArray();

        if (staged.Length == 0)
        {
            return;
        }

        text.AppendLine();
        text.AppendLine("### C.1 Stage breakdown");
        text.AppendLine();
        text.AppendLine("| Stage | Metric | Status | Value | n |");
        text.AppendLine("|---|---|---|---:|---:|");

        foreach (var metric in staged)
        {
            text.AppendLine(
                $"| {metric.Stage} | {metric.Name} | {metric.Status} | " +
                $"{(metric.Value is { } value ? value.ToString("0.000", CultureInfo.InvariantCulture) : "-")} | " +
                $"{metric.SampleSize} |");
        }
    }

    private static string Title(BiasSectionName section)
        => section switch
        {
            BiasSectionName.Fairness => "A. Fairness and outcome disparity",
            BiasSectionName.Recommendation => "B. Recommendation and retrieval bias",
            BiasSectionName.Reasoning => "C. Agent reasoning and decision bias",
            BiasSectionName.Language => "D. Language and content bias",
            _ => section.ToString(),
        };

    private static string JudgeSummary(EvaluationSummary summary)
        => !summary.JudgeConfigured
            ? "not configured"
            : summary.JudgeEvaluated == 0
                ? "configured; no eligible cases"
                : $"{summary.JudgePass}/{summary.JudgeEvaluated} passed ({Percentage(summary.JudgePassRate ?? 0)})";

    private static string JudgeCell(CaseResult testCase)
    {
        var judged = testCase.Repetitions.Where(repetition => repetition.JudgeEvaluated == true).ToArray();

        return judged.Length == 0
            ? "-"
            : string.Join("<br>", judged.Select(repetition => repetition.JudgePass == true ? "PASS" : "FAIL"));
    }

    private static string Escape(string? value)
        => (value ?? "-").Replace("|", "\\|", StringComparison.Ordinal);

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
