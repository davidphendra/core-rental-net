using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evaluation.AgentEval;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class AgentEvalRagContextProjectorTests
{
    [Fact]
    public void The_expected_setup_is_not_sent_as_retrieved_context()
    {
        var context = ContextFixture.Create(
            toolAnswers: [ContextFixture.ToolAnswer(EvaluationSlot.Desk, "D1")]);

        var projected = new AgentEvalRagContextProjector().Project(ContextFixture.Item(context));

        projected.Context.Should().Contain("D1");
        projected.Context.Should().NotContain("C1");
        projected.ExpectedOutput.Should().Contain("Chair");
        projected.ExpectedOutput.Should().Contain("C1");
    }

    [Fact]
    public void The_best_matching_variant_is_the_ground_truth()
    {
        var alternate = new ExpectedSetupVariant(
        [
            new ExpectedLine(EvaluationSlot.Desk, ["D2"]),
            new ExpectedLine(EvaluationSlot.Chair, ["C1"]),
        ]);

        var context = ContextFixture.Create(
            candidates:
            [
                new Candidate(
                [
                    new CandidateLine(EvaluationSlot.Desk, "D2", "Alternate desk", 1, 100),
                    new CandidateLine(EvaluationSlot.Chair, "C1", "Chair", 1, 100),
                ]),
            ],
            expectedVariants: [ContextFixture.DeskAndChair, alternate]);

        var projected = new AgentEvalRagContextProjector().Project(ContextFixture.Item(context));

        projected.ExpectedOutput.Should().Contain("D2");
        projected.ExpectedOutput.Should().NotContain("D1");
    }
}
