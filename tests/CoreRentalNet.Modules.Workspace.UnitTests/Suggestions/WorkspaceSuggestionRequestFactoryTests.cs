using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests.Suggestions;

public sealed class WorkspaceSuggestionRequestFactoryTests
{
    [Fact] // the sentence, the rules and the currency cross, and nothing else
    public void The_payload_is_the_sentence_and_the_rules_and_nothing_else()
    {
        var factory = new WorkspaceSuggestionRequestFactory(
            new TestCatalogService().Add("DSK0001", 800_000),
            new WorkspaceSlotSettings(Monitor: 2));

        var payload = factory.Create(new WorkspaceSuggestionQuery("  a desk and a chair  ", 1_500_000), "access-token");

        payload.Query.Should().Be("a desk and a chair");
        payload.Currency.Should().Be("IDR");
        payload.CeilingMonthly.Should().Be(1_500_000);
        payload.McpAccessToken.Should().Be("access-token");
        payload.Slots.Should().HaveCount(Enum.GetValues<SlotId>().Length);
        payload.Slots.Single(rule => rule.Slot is SlotId.Monitor).Capacity.Should().Be(2);
        payload.Slots.Single(rule => rule.Slot is SlotId.Desk).Capacity.Should().Be(1);
    }

    [Fact]
    public void An_empty_catalogue_still_states_a_currency()
    {
        var factory = new WorkspaceSuggestionRequestFactory(new TestCatalogService(), new WorkspaceSlotSettings());

        factory.Create(new WorkspaceSuggestionQuery("anything", null), string.Empty).Currency.Should().Be("IDR");
    }
}
