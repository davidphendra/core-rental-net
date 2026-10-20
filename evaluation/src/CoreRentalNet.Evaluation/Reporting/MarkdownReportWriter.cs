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
        text.AppendLine($"Rows: **{report.Summary.Rows}**  ");
        text.AppendLine($"Executions: **{report.Summary.Executions}**  ");
        text.AppendLine($"Case pass: **{report.Summary.CasePass}/{report.Summary.Rows} " +
                        $"({Percentage(report.Summary.CasePassRate)})**");
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
        text.AppendLine("| Row | Case | Stability | Case pass | Failure codes |");
        text.AppendLine("|---:|---|---|---|---|");

        foreach (var testCase in report.Cases)
        {
            text.AppendLine(
                $"| {testCase.RowNumber} | {testCase.CaseId} | {testCase.Stability} | " +
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

    private static string Percentage(double value)
        => value.ToString("P1", CultureInfo.InvariantCulture);
}
