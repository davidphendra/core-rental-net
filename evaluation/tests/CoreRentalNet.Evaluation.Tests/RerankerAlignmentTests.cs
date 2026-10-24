using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evidence;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class RerankerAlignmentTests
{
    [Fact]
    public void The_same_order_is_aligned()
    {
        var result = Evaluate(
            ["D1", "D2"],
            Rerank("D1", 1), Rerank("D2", 2));

        result.Aligned.Should().BeTrue();
        result.AlignedSlots.Should().ContainSingle().Which.Should().Be(EvaluationSlot.Desk);
    }

    [Fact]
    public void A_reordered_selection_is_aligned()
    {
        var result = Evaluate(
            ["D1", "D2"],
            Rerank("D2", 1), Rerank("D1", 2));

        result.Aligned.Should().BeTrue();
    }

    [Fact]
    public void A_same_order_subset_is_aligned()
    {
        var result = Evaluate(
            ["D1", "D2", "D3"],
            Rerank("D1", 1), Rerank("D2", 2));

        result.Aligned.Should().BeTrue();
    }

    [Fact]
    public void A_reordered_subset_is_aligned()
    {
        var result = Evaluate(
            ["D1", "D2", "D3"],
            Rerank("D3", 1), Rerank("D2", 2));

        result.Aligned.Should().BeTrue();
    }

    [Fact]
    public void An_unknown_sku_is_not_aligned()
    {
        var result = Evaluate(
            ["D1", "D2"],
            Rerank("D9", 1));

        result.Aligned.Should().BeFalse();
        result.HasUnknownSku.Should().BeTrue();
    }

    [Fact]
    public void An_empty_selection_is_not_aligned()
    {
        var result = Evaluate(["D1", "D2"]);

        result.Aligned.Should().BeFalse();
    }

    private static RerankerAlignmentResult Evaluate(
        string[] input,
        params RerankSelectionEvidence[] selections)
    {
        var context = ContextFixture.Create(
            toolAnswers: [ContextFixture.ToolAnswer(EvaluationSlot.Desk, input)],
            rerankSelections: selections);

        return RerankerAlignment.Evaluate(context);
    }

    private static RerankSelectionEvidence Rerank(string sku, int rank)
        => new(EvaluationSlot.Desk, sku, rank, null);
}
