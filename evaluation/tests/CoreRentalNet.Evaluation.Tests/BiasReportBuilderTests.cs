using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evidence;
using CoreRentalNet.Evaluation.Execution;
using CoreRentalNet.Evaluation.Reporting.Bias;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class BiasReportBuilderTests
{
    [Fact]
    public void All_four_sections_are_present_even_without_labels()
    {
        var report = Build(ContextFixture.Create());

        report.Metrics.Select(metric => metric.Section).Distinct()
            .Should().BeEquivalentTo(Enum.GetValues<BiasSectionName>());
        report.Metrics.Where(metric => metric.Section == BiasSectionName.Fairness)
            .Should().OnlyContain(metric => metric.Status == BiasMetricStatus.NotApplicable);
        report.Metrics.Where(metric => metric.Section == BiasSectionName.Language)
            .Should().OnlyContain(metric => metric.Status == BiasMetricStatus.NotApplicable);
    }

    [Fact]
    public void Recommendation_metrics_compute_from_existing_evidence()
    {
        var report = Build(ContextFixture.Create());

        report.Metrics.Single(metric => metric.Name == "Recommendation Diversity")
            .Value.Should().Be(1);
        report.Metrics.Single(metric => metric.Name == "Retrieval Coverage")
            .Value.Should().Be(1);
    }

    private static BiasReport Build(EvaluationContext context)
    {
        var testCase = new EvaluationCase(
            1,
            "TS-001",
            "a desk and a chair",
            [ContextFixture.DeskAndChair],
            null,
            null);
        var run = new RowRun(
            testCase,
            1,
            "run-1",
            [],
            new RunTrace("run-1", [], []));
        var repetition = new RepetitionResult(
            1,
            new Dictionary<string, bool>(StringComparer.Ordinal),
            [],
            1,
            1);
        var result = new CaseResult(
            1,
            testCase.CaseId,
            testCase.Sentence,
            [repetition],
            true,
            "1/1",
            [],
            1,
            1);

        return BiasReportBuilder.CreateDefault().Build(new BiasInput(
            [result],
            [new BiasRun(run, context)],
            [],
            ContextFixture.Catalogue));
    }
}
