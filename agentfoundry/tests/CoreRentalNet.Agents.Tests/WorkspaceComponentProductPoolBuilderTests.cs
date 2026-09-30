using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Shared.Mcp;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The pool is built from what the tools actually answered, and which component a product belongs to is read off
/// the arguments a search was made with.
/// </summary>
/// <remarks>
/// The description surviving is the point of the whole stage: it is the text that says what a product is, and the
/// text nothing before the reranker reads. The component is read off the search's own arguments rather than off the
/// answer, because a tool knows a catalogue category and the mapping is the one place that knows which component
/// that is.
/// </remarks>
public sealed class WorkspaceComponentProductPoolBuilderTests
{
    private static readonly WorkspaceComponentProductPoolBuilder PoolBuilder = new(CatalogueSearchToolNames.All);

    [Fact]
    public void A_catalogue_answer_becomes_products_with_their_descriptions()
    {
        var ledger = new McpToolAnswerLedger();
        ledger.Record(CatalogueSearchToolNames.NameSearch, SearchArguments("desk", null), DeskAnswer);

        var pool = PoolBuilder.BuildPoolFrom(ledger);

        pool.Should().ContainSingle();
        pool[0].Sku.Should().Be("DSKB08XN4JDR");
        pool[0].Description.Should().Be(
            "A height-adjustable sitting or standing desk.",
            "the description is what the reranker reasons over, and it is the tool's own text");
        pool[0].Slot.Should().Be(WorkspaceSlot.Desk);
    }

    [Fact] // the component comes from the search's arguments, so an accessory is not a desk
    public void The_component_is_read_off_the_arguments_the_search_was_made_with()
    {
        var ledger = new McpToolAnswerLedger();
        ledger.Record(CatalogueSearchToolNames.NameSearch, SearchArguments("accessory", "beanbag"), DeskAnswer);

        PoolBuilder.BuildPoolFrom(ledger)[0].Slot.Should().Be(WorkspaceSlot.RelaxZone);
    }

    [Fact] // a tool that is not the catalogue's is not read, and its answer is not a product
    public void An_answer_from_another_tool_is_not_read()
    {
        var ledger = new McpToolAnswerLedger();
        ledger.Record("some_other_tool", SearchArguments("desk", null), DeskAnswer);

        PoolBuilder.BuildPoolFrom(ledger).Should().BeEmpty();
    }

    [Fact] // a tool whose name says catalogue but whose answer is not the catalogue's contract is refused
    public void A_catalogue_answer_that_is_not_its_contract_is_refused()
    {
        var ledger = new McpToolAnswerLedger();
        ledger.Record(CatalogueSearchToolNames.NameSearch, SearchArguments("desk", null), "{\"nothing\": true}");

        var act = () => PoolBuilder.BuildPoolFrom(ledger);

        // The rows are required by the contract, so an answer without them does not deserialize at all — and a run
        // that quietly reranked an empty pool would report a catalogue that has nothing in it.
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void Neither_search_nor_products_means_an_empty_pool()
        => PoolBuilder.BuildPoolFrom(new McpToolAnswerLedger()).Should().BeEmpty();

    private static Dictionary<string, object?> SearchArguments(string category, string? subCategory)
        => new() { ["category"] = category, ["subCategory"] = subCategory };

    private const string DeskAnswer =
        """
        { "matches": { "value": [ { "sku": "DSKB08XN4JDR", "category": "desk", "name": "Sit-Stand Desk",
                                    "subCategory": null,
                                    "description": "A height-adjustable sitting or standing desk.",
                                    "pricePerMonth": 4200000 } ] },
          "cheapestProductIgnoringTheCeiling": null }
        """;
}
