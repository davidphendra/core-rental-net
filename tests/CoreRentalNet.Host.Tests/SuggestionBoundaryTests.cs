using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// AIWB-40: only the query and the catalogue cross to Foundry - no identity, no address, no draft state.
/// </summary>
/// <remarks>
/// Asserted as a SHAPE rather than as a string search, because a string search only catches the leak somebody
/// already thought of. A request with no property to put an account in cannot carry one, and a builder whose
/// only input is the customer's sentence cannot be handed anything else - so these hold as the code changes
/// rather than only for the payload somebody remembered to check.
/// </remarks>
public sealed class SuggestionBoundaryTests
{
    [Fact] // AIWB-40
    public void The_request_has_nowhere_to_put_a_customer()
    {
        // Exactly these, so a field added later is a decision about crossing the boundary rather than a
        // convenient place to put one.
        typeof(SuggestionRequest).GetProperties()
            .Select(property => property.Name)
            .Should().BeEquivalentTo(["RunId", "Query", "Currency", "CeilingMonthly", "Slots", "Catalogue"]);
    }

    [Fact] // AIWB-40, from the other side: what a builder is given cannot be a customer
    public void The_builder_is_given_the_catalogue_the_slots_the_sentence_and_the_shortlist()
    {
        // AMENDED BY e06, AND THE AMENDMENT IS THE POINT OF ASSERTING THIS AT ALL. It used to say Build took
        // "RunRequest" and nothing else - a deliberate over-statement, because a builder that can only be handed
        // the customer's sentence cannot be handed an account, an address or a draft. e06 gave it a second
        // argument, and the second argument had to be justified rather than slipped in: a shortlist of SKUs is
        // catalogue-derived and carries no identity, which is what keeps the rule's PURPOSE intact even though
        // its letter changed. What it must never become is a draft, an address or a customer, and the constructor
        // assertion below is what holds that line.
        var build = typeof(SuggestionRequestBuilder).GetMethod(nameof(SuggestionRequestBuilder.Build))!;
        var parameters = build.GetParameters();

        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be<RunRequest>();
        parameters[1].ParameterType.Should().Be<IReadOnlyList<ShortlistItem>>(
            "the shortlist is identifiers chosen by retrieval, and nothing about the customer travels with it");

        typeof(SuggestionRequestBuilder).GetConstructors().Single()
            .GetParameters().Select(parameter => parameter.ParameterType.Name)
            .Should().BeEquivalentTo(["IProductCatalog", "WorkspaceSlotSettings"]);
    }

    [Fact] // the consequence, on the bytes that actually travel
    public void The_payload_carries_no_identity_no_address_and_no_draft()
    {
        var catalogue = new TestCatalogue().Add("DSKB08XN4JDR", 266_000m);
        var builder = new SuggestionRequestBuilder(catalogue, new WorkspaceSlotSettings());

        var payload = SuggestionPayload.From(builder.Build(
            new RunRequest("a quiet corner", null),
            StubCatalogShortlist.Items("DSKB08XN4JDR")));

        payload.Json.Should().Contain("a quiet corner", "the customer's own words are the request");

        foreach (var absent in new[] { "Villa Lotus", "dewi@example.com", "auth0|", "deliveryAddress", "DraftToken" })
        {
            payload.Json.Should().NotContain(absent);
        }
    }
}
