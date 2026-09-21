using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using Json.Schema;
using Xunit;
using WorkspaceSlots = CoreRentalNet.Modules.Workspace.Domain.SlotId;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The payload a run sends: what the model is shown, and what it is no longer shown.
/// </summary>
/// <remarks>
/// This is where e06 reaches e05's passing code. The request used to carry the catalogue — all 205 products,
/// ~336 KB, ~86k tokens, pushed on both agent calls against a deployment that allows 100,000 tokens a minute.
/// It now carries what retrieval chose, and these tests hold both halves of that: the fourteen that travel, and
/// the contract they still satisfy.
/// </remarks>
public sealed class SuggestionRequestBuilderTests
{
    // The schemas are parsed once for the whole assembly, in SharedContracts: JsonSchema.FromText registers each
    // one globally by its $id, so a second class parsing them throws rather than merely repeats work.

    private static SuggestionRequestBuilder Builder(TestCatalogue catalogue)
        => new(catalogue, new WorkspaceSlotSettings());

    /// <summary>A catalogue of the size the real one is, so "fourteen" and "205" are distinguishable.</summary>
    private static TestCatalogue CatalogueOf205()
    {
        var catalogue = new TestCatalogue();

        for (var position = 0; position < 205; position++)
        {
            catalogue.Add($"SKU{position:D5}", 100_000m + position);
        }

        return catalogue;
    }

    /// <summary>Fourteen SKUs, deliberately NOT in the catalogue's order: retrieval's order is its own.</summary>
    private static string[] FourteenOutOfOrder()
        => [.. Enumerable.Range(0, 14).Reverse().Select(position => $"SKU{position:D5}")];

    [Fact] // SCR-15, SCR-17
    public void The_payload_carries_the_shortlist_and_not_the_whole_catalogue()
    {
        var catalogue = CatalogueOf205();
        var shortlist = FourteenOutOfOrder();

        var request = Builder(catalogue).Build(new RunRequest("a quiet corner", null), StubCatalogShortlist.Items(shortlist));

        request.Catalogue.Should().HaveCount(14);
        request.Catalogue.Should().NotContain(item => item.Sku == "SKU00204");

        // The whole catalogue is still what the payload is built FROM - its currency is the envelope's - so this
        // is not "the catalogue was replaced". It is "the catalogue is no longer pushed", which is the change.
        catalogue.All.Should().HaveCount(205);
    }

    [Fact] // SCR-15
    public void The_products_travel_in_the_order_retrieval_chose_them()
    {
        // Retrieval's order is nearest-first and it is the order the model reads. The stub's list is reversed
        // against the catalogue's, so a payload that came back sorted by SKU would fail here - which is the
        // whole difference between carrying a ranking and carrying a set.
        var shortlist = FourteenOutOfOrder();

        var request = Builder(CatalogueOf205()).Build(new RunRequest("a quiet corner", null), StubCatalogShortlist.Items(shortlist));

        request.Catalogue.Select(item => item.Sku).Should().Equal(shortlist);
    }

    [Fact] // SCR-16
    public void The_payload_still_satisfies_the_request_schema()
    {
        // The contract did not move: `catalogue` is still `catalogue`, with fewer items in it. If it HAD moved,
        // the schema under agentfoundry/shared/contracts would have had to move with it - and the two trees share
        // no project reference, so this test is the only thing that would notice.
        var request = Builder(CatalogueOf205())
            .Build(new RunRequest("a quiet corner for two monitors", 1_500_000), StubCatalogShortlist.Items(FourteenOutOfOrder()));

        var result = SharedContracts.Schemas["suggestion.request.schema.json"]
            .Evaluate(JsonSerializer.SerializeToElement(request, SuggestionJson.Options));

        result.IsValid.Should().BeTrue(JsonSerializer.Serialize(result));
    }

    [Fact] // SCR-16
    public void The_customer_s_ceiling_survives_into_the_payload()
    {
        // The ceiling is the customer's own statement and nothing retrieval does may drop it. The alternative is
        // a run that quietly ignores a budget the customer named.
        var request = Builder(CatalogueOf205())
            .Build(new RunRequest("a quiet corner", 1_500_000), StubCatalogShortlist.Items(FourteenOutOfOrder()));

        request.CeilingMonthly.Should().Be(1_500_000);
    }

    [Fact] // SCR-16
    public void The_slot_rules_still_come_from_the_domain_and_not_from_the_shortlist()
    {
        // Seven buckets and seven slots are not the same vocabulary even though they partition the same
        // catalogue. The rules the model is told are the domain's, so a slot its products did not reach still
        // gets its rule: the model is told what the application will accept, not what it happens to be able to
        // choose from.
        var request = Builder(CatalogueOf205())
            .Build(new RunRequest("a quiet corner", null), StubCatalogShortlist.Items(FourteenOutOfOrder()));

        request.Slots.Select(rule => rule.Slot).Should().BeEquivalentTo(Enum.GetValues<WorkspaceSlots>());
    }

    [Fact] // SCR-15
    public void A_shortlisted_sku_the_catalogue_does_not_hold_is_refused()
    {
        // The index describing a catalogue that has changed. Retrieval already refuses this; refusing it again
        // here is deliberate, because the alternative is a payload quietly missing a product the model was told
        // the catalogue contained.
        var catalogue = new TestCatalogue().Add("DSKB08XN4JDR", 266_000m);

        var act = () => Builder(catalogue)
            .Build(new RunRequest("a quiet corner", null), StubCatalogShortlist.Items("DSKB08XN4JDR", "GONE00000000"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*GONE00000000*CatalogIngestion*");
    }
}
