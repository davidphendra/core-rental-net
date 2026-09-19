using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;
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
    public void The_builder_is_given_the_catalogue_the_slots_and_the_customer_s_sentence()
    {
        var build = typeof(SuggestionRequestBuilder).GetMethod(nameof(SuggestionRequestBuilder.Build))!;

        build.GetParameters().Select(parameter => parameter.ParameterType.Name)
            .Should().Equal("RunRequest");

        typeof(SuggestionRequestBuilder).GetConstructors().Single()
            .GetParameters().Select(parameter => parameter.ParameterType.Name)
            .Should().BeEquivalentTo(["IProductCatalog", "WorkspaceSlotSettings"]);
    }

    [Fact] // the consequence, on the bytes that actually travel
    public void The_payload_carries_no_identity_no_address_and_no_draft()
    {
        var catalogue = new TestCatalogue().Add("DSKB08XN4JDR", 266_000m);
        var builder = new SuggestionRequestBuilder(catalogue, new WorkspaceSlotSettings());

        var payload = SuggestionPayload.From(builder.Build(new RunRequest("a quiet corner", null)));

        payload.Json.Should().Contain("a quiet corner", "the customer's own words are the request");

        foreach (var absent in new[] { "Villa Lotus", "dewi@example.com", "auth0|", "deliveryAddress", "DraftToken" })
        {
            payload.Json.Should().NotContain(absent);
        }
    }
}
