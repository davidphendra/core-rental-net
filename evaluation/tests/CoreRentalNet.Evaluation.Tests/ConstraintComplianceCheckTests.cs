using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class ConstraintComplianceCheckTests
{
    [Fact]
    public void A_grounded_candidate_within_capacity_passes()
    {
        var check = new ConstraintComplianceCheck(ContextFixture.Catalogue).Create();

        check(ContextFixture.Item(ContextFixture.Create())).Passed.Should().BeTrue();
    }

    [Fact]
    public void A_missing_requested_slot_fails()
    {
        var candidate = new Candidate(
        [
            new CandidateLine(EvaluationSlot.Desk, "D1", "Desk", 1, 100),
        ]);

        var result = new ConstraintComplianceCheck(ContextFixture.Catalogue)
            .Create()(ContextFixture.Item(ContextFixture.Create(candidates: [candidate])));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.ConstraintMissingSlot(EvaluationSlot.Chair));
    }

    [Fact]
    public void An_unknown_sku_fails()
    {
        var candidate = new Candidate(
        [
            new CandidateLine(EvaluationSlot.Desk, "D9", "Unknown", 1, 100),
            new CandidateLine(EvaluationSlot.Chair, "C1", "Chair", 1, 100),
        ]);

        var result = new ConstraintComplianceCheck(ContextFixture.Catalogue)
            .Create()(ContextFixture.Item(ContextFixture.Create(candidates: [candidate])));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.ConstraintUnknownSku("D9"));
    }

    [Fact]
    public void A_sku_that_was_not_retrieved_fails()
    {
        var candidate = new Candidate(
        [
            new CandidateLine(EvaluationSlot.Desk, "D1", "Desk", 1, 100),
            new CandidateLine(EvaluationSlot.Chair, "C1", "Chair", 1, 100),
        ]);
        var toolAnswers = new[] { ContextFixture.ToolAnswer(EvaluationSlot.Desk, "D1") };

        var result = new ConstraintComplianceCheck(ContextFixture.Catalogue)
            .Create()(ContextFixture.Item(ContextFixture.Create(
                candidates: [candidate],
                toolAnswers: toolAnswers)));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.ConstraintNotRetrieved("C1"));
    }
}
