using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Evidence.Trace;
using CoreRentalNet.Evaluation.Evidence.Projection;
using CoreRentalNet.Evaluation.Evidence.Context;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Evidence;

public sealed class WorkspaceTraceProjectorTests
{
    [Fact]
    public void A_catalogue_answer_is_attributed_to_its_slot_and_skus()
    {
        var trace = new RunTrace(
            "run-1",
            StageCalls: [],
            ToolExchanges:
            [
                new TraceToolExchange(
                    "catalogue-product-retriever",
                    1,
                    "call-1",
                    WorkspaceToolNames.NameSearch,
                    new Dictionary<string, object?>
                    {
                        ["category"] = "accessory",
                        ["subCategory"] = "monitor",
                    },
                    """{"matches":{"value":[{"sku":"M1"},{"sku":"M2"}]}}"""),
            ]);

        var evidence = new WorkspaceTraceProjector().Project(trace);

        evidence.ToolAnswers.Should().ContainSingle();
        evidence.ToolAnswers[0].Slot.Should().Be(EvaluationSlot.Monitor);
        evidence.ToolAnswers[0].ReturnedSkus.Should().Equal("M1", "M2");
    }

    [Fact]
    public void The_reranker_top_selection_is_projected_in_rank_order()
    {
        const string Ranking =
            """
            {"categories":{
              "desk":[{"sku":"D1","relevance":"high"},{"sku":"D2","relevance":"medium"},{"sku":"D3","relevance":"low"},{"sku":"D4","relevance":"low"}],
              "chair":[],
              "monitor":[],
              "lamp":[],
              "plant":[],
              "bean_bag":[],
              "coffee_machine":[]}}
            """;

        var trace = new RunTrace(
            "run-1",
            StageCalls: [new TraceStageCall(WorkspaceStageNames.Reranker, 1, Ranking)],
            ToolExchanges: []);

        var evidence = new WorkspaceTraceProjector(maximumSelectedProductsPerComponent: 3).Project(trace);

        evidence.RerankSelections.Should().HaveCount(3);
        evidence.RerankSelections.Select(selection => selection.Sku).Should().Equal("D1", "D2", "D3");
        evidence.RerankSelections.Select(selection => selection.Rank).Should().Equal(1, 2, 3);
    }

    [Theory]
    [InlineData("desk", null, "Desk")]
    [InlineData("chair", null, "Chair")]
    [InlineData("accessory", "monitor", "Monitor")]
    [InlineData("accessory", "lamp", "Lamp")]
    [InlineData("accessory", "plant", "Plant")]
    [InlineData("accessory", "beanbag", "RelaxZone")]
    [InlineData("accessory", "coffee", "CoffeeStation")]
    public void The_component_mapping_matches_the_agent_contract(
        string category,
        string? subCategory,
        string expectedSlot)
    {
        EvaluationComponentMapping.SlotFor(category, subCategory).Should().Be(expectedSlot);
    }
}
