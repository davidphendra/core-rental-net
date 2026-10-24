using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class CasePassPolicyRerankerTests
{
    [Fact]
    public void A_reranker_aligned_case_passes_even_when_exact_metrics_fail()
    {
        var metrics = MetricNames.All.ToDictionary(metric => metric, _ => false);

        CasePassPolicy.Passed(metrics, rerankerAligned: true, timedOut: false)
            .Should().BeTrue();
    }

    [Fact]
    public void A_timeout_never_passes_even_when_reranker_aligned()
    {
        var metrics = MetricNames.All.ToDictionary(metric => metric, _ => true);

        CasePassPolicy.Passed(metrics, rerankerAligned: true, timedOut: true)
            .Should().BeFalse();
    }

    [Fact]
    public void An_unaligned_case_keeps_the_exact_metric_policy()
    {
        var metrics = MetricNames.All.ToDictionary(metric => metric, _ => false);

        CasePassPolicy.Passed(metrics, rerankerAligned: false, timedOut: false)
            .Should().BeFalse();
    }
}
