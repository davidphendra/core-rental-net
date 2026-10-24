using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using Microsoft.Extensions.AI.Evaluation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class LocalJudgeEvaluatorTests
{
    [Fact]
    public async Task Exact_cases_skip_the_judge()
    {
        var evaluator = Create("""{"verdict":"pass","confidence":0.9,"reason":"ok"}""");

        var result = await EvaluatorResult(evaluator, ContextFixture.Create());

        Boolean(result, MetricNames.JudgeEvaluated).Should().BeFalse();
        result.Metrics.Should().NotContainKey(MetricNames.JudgePass);
    }

    [Fact]
    public async Task An_aligned_mismatch_uses_the_judge()
    {
        var evaluator = Create("""{"verdict":"pass","confidence":0.9,"reason":"Equivalent product."}""");
        var context = ContextFixture.Create(
            candidates: [new Candidate(
            [
                new CandidateLine(EvaluationSlot.Desk, "D2", "Alternate desk", 1, 100),
                new CandidateLine(EvaluationSlot.Chair, "C1", "Chair", 1, 100),
            ])]);

        var result = await EvaluatorResult(evaluator, context);

        Boolean(result, MetricNames.JudgeEvaluated).Should().BeTrue();
        Boolean(result, MetricNames.JudgePass).Should().BeTrue();
        result.Metrics[MetricNames.JudgeConfidence].Should().BeOfType<NumericMetric>()
            .Which.Value.Should().Be(0.9);
    }

    [Fact]
    public async Task A_judge_failure_is_recorded_as_an_error()
    {
        var evaluator = Create("not json");
        var context = ContextFixture.Create(
            candidates: [new Candidate(
            [
                new CandidateLine(EvaluationSlot.Desk, "D2", "Alternate desk", 1, 100),
                new CandidateLine(EvaluationSlot.Chair, "C1", "Chair", 1, 100),
            ])]);

        var result = await EvaluatorResult(evaluator, context);

        Boolean(result, MetricNames.JudgeEvaluated).Should().BeTrue();
        Boolean(result, MetricNames.JudgePass).Should().BeFalse();
        result.Metrics[MetricNames.JudgePass].Reason.Should().StartWith("JUDGE_ERROR");
    }

    private static LocalJudgeEvaluator Create(string reply)
        => new(
            new FixedJudgeChatClient(reply),
            TimeSpan.FromSeconds(2),
            new JudgePromptBuilder(),
            new JudgeVerdictParser());

    private static async Task<EvaluationResult> EvaluatorResult(
        LocalJudgeEvaluator evaluator,
        Evidence.EvaluationContext context)
    {
        var results = await evaluator.EvaluateAsync(
            [ContextFixture.Item(context)],
            evaluator.Name,
            CancellationToken.None);

        return results.Items[0];
    }

    private static bool Boolean(EvaluationResult result, string metric)
        => ((BooleanMetric)result.Metrics[metric]).Value == true;
}
