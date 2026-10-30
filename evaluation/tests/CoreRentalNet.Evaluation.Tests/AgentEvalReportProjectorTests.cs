using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Reporting;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class AgentEvalReportProjectorTests
{
    [Fact]
    public void The_report_carries_measured_scores_and_not_measured_metrics()
    {
        var report = Report(
            Case(1, pass: true),
            Row(1, MetricNames.LlmFaithfulness, 90, measured: true),
            Row(1, MetricNames.LlmContextRecall, null, measured: false));

        var projected = AgentEvalReportProjector.Project(report, "agent");

        projected.TotalTests.Should().Be(1);
        projected.PassedTests.Should().Be(1);
        projected.TestResults[0].MetricScores[MetricNames.LlmFaithfulness].Should().Be(90);
        projected.TestResults[0].MetricsNotMeasured.Should().ContainKey(MetricNames.LlmContextRecall);
    }

    [Fact]
    public void Print_writes_the_agent_eval_summary()
    {
        var report = AgentEvalReportProjector.Project(
            Report(
                Case(1, pass: true),
                Row(1, MetricNames.LlmFaithfulness, 90, measured: true)),
            "agent");
        var writer = new StringWriter();
        var original = Console.Out;

        try
        {
            Console.SetOut(writer);
            report.Print();
        }
        finally
        {
            Console.SetOut(original);
        }

        writer.ToString().Should().Contain("AgentEval Results");
    }

    [Fact]
    public async Task The_markdown_export_is_written_beside_the_domain_report()
    {
        var report = AgentEvalReportProjector.Project(
            Report(
                Case(1, pass: true),
                Row(1, MetricNames.LlmFaithfulness, 90, measured: true)),
            "agent");
        var path = Path.Combine(Path.GetTempPath(), $"agenteval-{Guid.NewGuid():N}.json");

        try
        {
            await AgentEvalReportExporter.WriteAsync(
                report,
                path,
                ["markdown"],
                CancellationToken.None);

            var exported = Path.ChangeExtension(path, ".agenteval.md");
            File.Exists(exported).Should().BeTrue();
            (await File.ReadAllTextAsync(exported)).Should().Contain("AgentEval Results");
        }
        finally
        {
            foreach (var file in Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + "*"))
            {
                File.Delete(file);
            }
        }
    }

    private static MetricRow Row(int row, string metric, double? value, bool measured)
        => new(
            RunId: $"run-{row}",
            RowNumber: row,
            Repetition: 1,
            Provider: "test",
            Metric: metric,
            Passed: value >= 70,
            Value: value,
            Reason: measured ? null : "not measured")
        {
            Measured = measured,
        };

    private static CaseResult Case(int row, bool pass)
        => new(
            RowNumber: row,
            CaseId: $"TS-{row:000}",
            Sentence: "a desk",
            Repetitions:
            [
                new RepetitionResult(
                    Repetition: 1,
                    Metrics: new Dictionary<string, bool>(),
                    FailureCodes: [],
                    AnswerF1: 1,
                    RetrievalRecall: 1),
            ],
            CasePass: pass,
            Stability: "1/1",
            FailureCodes: [],
            MeanAnswerF1: 1,
            MeanRetrievalRecall: 1)
        {
            ActualOutput = "D1",
        };

    private static EvaluationReport Report(CaseResult testCase, params MetricRow[] metrics)
        => new(
            Cases: [testCase],
            Metrics: metrics,
            Summary: new EvaluationSummary(
                Rows: 1,
                Executions: 1,
                CasePass: testCase.CasePass ? 1 : 0,
                CasePassRate: testCase.CasePass ? 1 : 0,
                MetricPassRates: new Dictionary<string, double>(),
                Stability: new Dictionary<string, int>()))
        {
            Scope = EvaluationScope.Tiny,
            TotalRows = 200,
        };
}
