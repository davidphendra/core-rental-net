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

public sealed class RawRetrievalQualityCheckTests
{
    [Fact]
    public void Every_expected_sku_returned_by_a_search_passes()
    {
        var context = ContextFixture.Create();

        RawRetrievalQualityCheck.Create()(ContextFixture.Item(context)).Passed.Should().BeTrue();
    }

    [Fact]
    public void A_product_returned_by_a_search_but_not_reranked_passes_raw_recall()
    {
        var context = ContextFixture.Create(
            rerankSelections:
            [
                new RerankSelectionEvidence(EvaluationSlot.Desk, "D1", 1, null),
            ]);

        RawRetrievalQualityCheck.Create()(ContextFixture.Item(context)).Passed.Should().BeTrue();
        RetrievalQualityCheck.Create()(ContextFixture.Item(context)).Passed.Should().BeFalse();
    }

    [Fact]
    public void A_product_never_returned_by_a_search_fails_raw_recall()
    {
        var context = ContextFixture.Create(
            toolAnswers: [ContextFixture.ToolAnswer(EvaluationSlot.Desk, "D1")]);

        var result = RawRetrievalQualityCheck.Create()(ContextFixture.Item(context));

        result.Passed.Should().BeFalse();
        result.Reason.Should().Contain(FailureCodes.RetrievalMiss("C1"));
    }
}
