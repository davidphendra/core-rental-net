using AgentEval.Comparison;
using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class StochasticSummaryFactoryTests
{
    [Fact]
    public void The_summary_uses_agent_eval_statistics_and_verdict()
    {
        var report = Report(
            Row(1, 80, passed: true),
            Row(2, 90, passed: true),
            Row(3, 100, passed: true));

        var summary = StochasticSummaryFactory.Create(report, 0.8);
        var metric = summary.Metrics.Single();

        metric.Runs.Should().Be(3);
        metric.Measured.Should().Be(3);
        metric.NotMeasured.Should().Be(0);
        metric.Passed.Should().Be(3);
        metric.Mean.Should().Be(90);
        metric.StandardDeviation.Should().Be(10);
        metric.Verdict.Should().Be(StochasticVerdict.Passed);
    }

    [Fact]
    public void A_not_measured_run_can_leave_the_verdict_undecided()
    {
        var report = Report(
            Row(1, 90, passed: true),
            Row(2, 90, passed: true),
            Row(3, null, passed: false, measured: false));

        var summary = StochasticSummaryFactory.Create(report, 0.8);
        var metric = summary.Metrics.Single();

        metric.Measured.Should().Be(2);
        metric.NotMeasured.Should().Be(1);
        metric.PassRate.Should().BeApproximately(2d / 3d, 0.0001);
        metric.Mean.Should().Be(90);
        metric.Verdict.Should().Be(StochasticVerdict.Undecided);
    }

    [Fact]
    public void A_threshold_outside_zero_to_one_is_refused()
    {
        var report = Report(Row(1, 90, passed: true));

        var act = () => StochasticSummaryFactory.Create(report, 1.5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static MetricRow Row(
        int repetition,
        double? value,
        bool passed,
        bool measured = true)
        => new(
            RunId: $"run-{repetition}",
            RowNumber: 1,
            Repetition: repetition,
            Provider: "test",
            Metric: MetricNames.LlmFaithfulness,
            Passed: passed,
            Value: value,
            Reason: null)
        {
            Measured = measured,
        };

    private static EvaluationReport Report(params MetricRow[] rows)
        => new(
            Cases: [],
            Metrics: rows,
            Summary: new EvaluationSummary(
                Rows: 0,
                Executions: rows.Length,
                CasePass: 0,
                CasePassRate: 0,
                MetricPassRates: new Dictionary<string, double>(),
                Stability: new Dictionary<string, int>()))
        {
            Scope = EvaluationScope.Tiny,
            TotalRows = 200,
        };
}
