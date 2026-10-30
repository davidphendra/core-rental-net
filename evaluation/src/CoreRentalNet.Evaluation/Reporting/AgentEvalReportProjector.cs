using AgentEval.Models;
using CoreRentalNet.Evaluation.Evaluation;
using DomainReport = CoreRentalNet.Evaluation.Evaluation.EvaluationReport;

namespace CoreRentalNet.Evaluation.Reporting;

/// <summary>Projects the domain report into AgentEval's report model for printing and export.</summary>
public static class AgentEvalReportProjector
{
    public static AgentEval.Models.EvaluationReport Project(
        DomainReport report,
        string? agentName = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        var rowsByCase = report.Metrics
            .GroupBy(row => row.RowNumber)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var hasRag = report.RagConfigured
                     || report.Metrics.Any(row => MetricNames.RagMetrics.Contains(row.Metric));
        var results = report.Cases
            .Select(testCase => Summary(
                testCase,
                rowsByCase.GetValueOrDefault(testCase.RowNumber) ?? [],
                hasRag))
            .ToList();
        var measured = results.Where(result => !result.NotMeasured).ToArray();

        return new AgentEval.Models.EvaluationReport
        {
            Name = "Workspace Suggestion Evaluation",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow,
            TotalTests = results.Count,
            PassedTests = results.Count(result => result.Passed),
            FailedTests = results.Count(result => !result.Passed && !result.NotMeasured),
            SkippedTests = results.Count(result => result.NotMeasured),
            OverallScore = measured.Length == 0
                ? null
                : measured.Average(result => result.Score ?? 0),
            TestResults = results,
            Agent = agentName is null ? null : new AgentInfo { Name = agentName },
            Metadata = new Dictionary<string, string>
            {
                ["scope"] = report.Scope.ToString(),
                ["totalRows"] = report.TotalRows.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["policyDigest"] = report.PolicyDigest ?? "unrecorded",
            },
        };
    }

    private static TestResultSummary Summary(
        CaseResult testCase,
        IReadOnlyList<MetricRow> rows,
        bool hasRag)
    {
        var measured = rows.Where(row => row.Measured && row.Value is not null).ToArray();
        var scores = measured
            .GroupBy(row => row.Metric, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Average(row => row.Value!.Value),
                StringComparer.Ordinal);
        var notMeasured = rows
            .Where(row => !row.Measured)
            .GroupBy(row => row.Metric, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First().Reason ?? "not measured",
                StringComparer.Ordinal);
        var notMeasuredCase = measured.Length == 0;
        var scoreRows = hasRag
            ? measured.Where(row => MetricNames.RagMetrics.Contains(row.Metric)).ToArray()
            : [];
        var score = scoreRows.Length > 0
            ? scoreRows.Average(row => row.Value!.Value)
            : testCase.CasePass ? 100 : 0;

        return new TestResultSummary
        {
            Name = $"{testCase.RowNumber}: {testCase.CaseId}",
            Category = testCase.CaseId,
            Score = notMeasuredCase ? null : score,
            Passed = testCase.CasePass,
            NotMeasured = notMeasuredCase,
            Skipped = notMeasuredCase,
            DurationMs = testCase.Repetitions
                .Select(repetition => repetition.Timing?.TotalMs)
                .FirstOrDefault(value => value is not null) is { } total
                ? (long)total
                : null,
            Output = testCase.ActualOutput,
            MetricScores = scores,
            MetricsNotMeasured = notMeasured,
        };
    }
}
