using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evaluation.AgentEval;
using Microsoft.Extensions.AI.Evaluation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class AgentEvalRagEvaluatorTests
{
    private const string JudgeReply = """{"score":90,"reasoning":"supported"}""";

    [Fact]
    public async Task The_four_rag_metrics_are_measured()
    {
        var results = await Evaluate(ContextFixture.Create());

        foreach (var metric in MetricNames.RagMetrics)
        {
            results.Metrics.Keys.Should().Contain(metric);
            var value = (NumericMetric)results.Metrics[metric];
            value.Value.Should().NotBeNull();
            value.Interpretation!.Failed.Should().BeFalse();
        }
    }

    [Fact]
    public async Task A_missing_retrieved_context_is_not_measured()
    {
        var results = await Evaluate(ContextFixture.Create(toolAnswers: [], rerankSelections: []));

        var faithfulness = (NumericMetric)results.Metrics[MetricNames.LlmFaithfulness];
        faithfulness.Value.Should().BeNull();
        faithfulness.Interpretation!.Failed.Should().BeTrue();
        faithfulness.Reason.Should().Contain("not measured");

        var correctness = (NumericMetric)results.Metrics[MetricNames.LlmAnswerCorrectness];
        correctness.Value.Should().NotBeNull();
    }

    private static async Task<EvaluationResult> Evaluate(Evidence.EvaluationContext context)
    {
        var evaluator = new ProjectingEvaluator(
            AgentEvalRagEvaluatorFactory.Create(new FixedJudgeChatClient(JudgeReply)),
            new AgentEvalRagContextProjector());

        var results = await evaluator.EvaluateAsync(
            [ContextFixture.Item(context)],
            evaluator.Name,
            CancellationToken.None);

        return results.Items[0];
    }
}
