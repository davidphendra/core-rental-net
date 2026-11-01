using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
using Xunit;

using CoreRentalNet.Evaluation.Tests.Fixtures;

namespace CoreRentalNet.Evaluation.Tests.Checks;

public sealed class ToolCallAccuracyCheckTests
{
    [Fact]
    public void A_search_for_every_expected_component_passes()
    {
        ToolCallAccuracyCheck.Create()(ContextFixture.Item(ContextFixture.Create()))
            .Passed.Should().BeTrue();
    }

    [Fact]
    public void A_missing_component_search_fails()
    {
        var context = ContextFixture.Create(
            toolAnswers: [ContextFixture.ToolAnswer(EvaluationSlot.Desk, "D1")]);

        var result = ToolCallAccuracyCheck.Create()(ContextFixture.Item(context));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.ToolNotCalled(EvaluationSlot.Chair));
    }

    [Fact]
    public void An_unexpected_component_search_fails()
    {
        var context = ContextFixture.Create(
            toolAnswers:
            [
                ContextFixture.ToolAnswer(EvaluationSlot.Desk, "D1"),
                ContextFixture.ToolAnswer(EvaluationSlot.Chair, "C1"),
                ContextFixture.ToolAnswer(EvaluationSlot.Lamp, "L1"),
            ]);

        var result = ToolCallAccuracyCheck.Create()(ContextFixture.Item(context));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.ToolUnexpectedCall(EvaluationSlot.Lamp));
    }
}
