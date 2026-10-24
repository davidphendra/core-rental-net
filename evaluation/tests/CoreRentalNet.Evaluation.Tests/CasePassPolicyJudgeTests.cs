using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class CasePassPolicyJudgeTests
{
    [Fact]
    public void A_judge_pass_allows_an_aligned_case()
    {
        var metrics = HardGate();

        CasePassPolicy.Passed(metrics, rerankerAligned: true, timedOut: false, judgeEvaluated: true, judgePass: true)
            .Should().BeTrue();
    }

    [Fact]
    public void A_judge_fail_rejects_an_aligned_case()
    {
        var metrics = HardGate();

        CasePassPolicy.Passed(metrics, rerankerAligned: true, timedOut: false, judgeEvaluated: true, judgePass: false)
            .Should().BeFalse();
    }

    [Fact]
    public void A_judge_pass_cannot_rescue_a_hard_gate_failure()
    {
        var metrics = HardGate();
        metrics[MetricNames.ConstraintCompliance] = false;

        CasePassPolicy.Passed(metrics, rerankerAligned: true, timedOut: false, judgeEvaluated: true, judgePass: true)
            .Should().BeFalse();
    }

    [Fact]
    public void No_judge_preserves_the_aligned_policy()
    {
        var metrics = HardGate();

        CasePassPolicy.Passed(metrics, rerankerAligned: true, timedOut: false)
            .Should().BeTrue();
    }

    private static Dictionary<string, bool> HardGate()
        => new(StringComparer.Ordinal)
        {
            [MetricNames.ConstraintCompliance] = true,
            [MetricNames.ToolCallAccuracy] = true,
            [MetricNames.WorkflowCorrectness] = true,
        };
}
