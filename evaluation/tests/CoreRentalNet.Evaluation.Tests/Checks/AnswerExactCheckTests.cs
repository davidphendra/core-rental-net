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

public sealed class AnswerExactCheckTests
{
    [Fact]
    public void An_exact_sku_set_passes()
    {
        var result = AnswerExactCheck.Create()(ContextFixture.Item(ContextFixture.Create()));

        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void A_wrong_sku_fails_and_names_the_slot()
    {
        var candidate = new Candidate(
        [
            new CandidateLine(EvaluationSlot.Desk, "D9", "Wrong desk", 1, 100),
            new CandidateLine(EvaluationSlot.Chair, "C1", "Chair", 1, 100),
        ]);

        var result = AnswerExactCheck.Create()(ContextFixture.Item(
            ContextFixture.Create(candidates: [candidate])));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.WrongSku(EvaluationSlot.Desk));
    }

    [Fact]
    public void A_missing_slot_fails()
    {
        var candidate = new Candidate(
        [
            new CandidateLine(EvaluationSlot.Desk, "D1", "Desk", 1, 100),
        ]);

        var result = AnswerExactCheck.Create()(ContextFixture.Item(
            ContextFixture.Create(candidates: [candidate])));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.MissingSlot(EvaluationSlot.Chair));
    }

    [Fact]
    public void An_extra_slot_fails()
    {
        var candidate = new Candidate(
        [
            new CandidateLine(EvaluationSlot.Desk, "D1", "Desk", 1, 100),
            new CandidateLine(EvaluationSlot.Chair, "C1", "Chair", 1, 100),
            new CandidateLine(EvaluationSlot.Lamp, "L1", "Lamp", 1, 100),
        ]);

        var result = AnswerExactCheck.Create()(ContextFixture.Item(
            ContextFixture.Create(candidates: [candidate])));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.ExtraSlot(EvaluationSlot.Lamp));
    }
}
