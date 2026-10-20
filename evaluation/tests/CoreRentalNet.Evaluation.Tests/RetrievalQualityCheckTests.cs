using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using CoreRentalNet.Evaluation.Evaluation;
using CoreRentalNet.Evaluation.Evidence;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

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
