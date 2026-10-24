using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evidence;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class RerankerAlignmentCheckTests
{
    [Fact]
    public void A_selection_from_the_input_passes()
    {
        var context = ContextFixture.Create(
            toolAnswers: [ContextFixture.ToolAnswer(EvaluationSlot.Desk, "D1", "D2")],
            rerankSelections:
            [
                new RerankSelectionEvidence(EvaluationSlot.Desk, "D2", 1, null),
                new RerankSelectionEvidence(EvaluationSlot.Desk, "D1", 2, null),
            ]);

        var result = RerankerAlignmentCheck.Create()(ContextFixture.Item(context));

        result.Passed.Should().BeTrue();
        result.Reason.Should().Contain(EvaluationSlot.Desk);
    }

    [Fact]
    public void A_selection_outside_the_input_fails()
    {
        var context = ContextFixture.Create(
            toolAnswers: [ContextFixture.ToolAnswer(EvaluationSlot.Desk, "D1")],
            rerankSelections:
            [
                new RerankSelectionEvidence(EvaluationSlot.Desk, "D9", 1, null),
            ]);

        var result = RerankerAlignmentCheck.Create()(ContextFixture.Item(context));

        result.Passed.Should().BeFalse();
    }
}
