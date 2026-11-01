using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Runs;

/// <summary>The old pipeline's judge rows enter the new record under their report names, or not at all.</summary>
public sealed class AgentEvalMetricImportTests
{
    [Fact]
    public void The_rag_metrics_are_renamed_for_the_report()
    {
        var imported = AgentEvalMetricImport.Import(
        [
            Row(MetricNames.LlmFaithfulness, 0.9),
            Row(MetricNames.LlmContextRecall, 0.7),
        ]);

        imported.Select(metric => metric.Metric).Should().Equal("faithfulness", "context_recall");
        imported[0].Score.Should().BeApproximately(0.9, 1e-9);
    }

    [Fact]
    public void A_metric_the_judge_never_measured_is_dropped_rather_than_scored_zero()
    {
        var imported = AgentEvalMetricImport.Import(
        [
            new MetricRow("run", 1, 1, "agenteval", MetricNames.LlmFaithfulness, false, null, null) { Measured = false },
        ]);

        imported.Should().BeEmpty();
    }

    [Fact]
    public void A_metric_outside_the_import_list_is_ignored()
        => AgentEvalMetricImport.Import([Row(MetricNames.WorkflowCorrectness, 1)]).Should().BeEmpty();

    private static MetricRow Row(string metric, double value)
        => new("run", 1, 1, "agenteval", metric, value >= 0.5, value, null);
}
