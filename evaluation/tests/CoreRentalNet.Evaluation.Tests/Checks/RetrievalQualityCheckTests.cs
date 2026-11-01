using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using Xunit;

using CoreRentalNet.Evaluation.Tests.Fixtures;

namespace CoreRentalNet.Evaluation.Tests.Checks;

public sealed class RetrievalQualityCheckTests
{
    [Fact]
    public void Every_expected_sku_selected_passes()
    {
        var context = ContextFixture.Create(
            rerankSelections:
            [
                new RerankSelectionEvidence(EvaluationSlot.Desk, "D1", 1, null),
                new RerankSelectionEvidence(EvaluationSlot.Chair, "C1", 1, null),
            ]);

        RetrievalQualityCheck.Create()(ContextFixture.Item(context)).Passed.Should().BeTrue();
    }

    [Fact]
    public void A_missing_selected_sku_fails()
    {
        var context = ContextFixture.Create(
            rerankSelections:
            [
                new RerankSelectionEvidence(EvaluationSlot.Desk, "D1", 1, null),
            ]);

        var result = RetrievalQualityCheck.Create()(ContextFixture.Item(context));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.RetrievalMiss("C1"));
    }
}
