using System.Runtime.CompilerServices;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;
using CoreRentalNet.Modules.Workspace.Application.Services;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

/// <summary>
/// The rules between what the agent said and what a customer is shown.
/// </summary>
/// <remarks>
/// The agent is a fake, because these are the application's rules rather than the agent's behaviour:
/// every SKU is resolved against a catalogue and every price is that catalogue's, whatever the agent
/// believed it had read.
/// </remarks>
public sealed class SuggestionTests
{
    [Fact] // AIB-06
    public async Task A_candidate_is_priced_from_the_catalogue_and_not_from_the_agent()
    {
        var catalog = new TestCatalog()
            .Add("MON0001", 350_000m, CatalogCategory.Accessory, CatalogSubCategory.Monitor, "A monitor")
            .Add("DSK0001", 800_000m, CatalogCategory.Desk, null, "A desk");

        var suggestion = await SuggestAsync(
            new FakeAgent().Answering(Ok(Option("low", ("Monitor", "MON0001", 2), ("Desk", "DSK0001", 1)))),
            catalog);

        var lines = suggestion.Options.Single().Lines;

        lines.Should().HaveCount(2);
        lines.Single(line => line.Sku == "MON0001").UnitMonthlyPrice.Amount.Should().Be(350_000m);
        lines.Single(line => line.Sku == "MON0001").Name.Should().Be("A monitor");
        lines.Single(line => line.Sku == "MON0001").Quantity.Should().Be(2, "the agent's quantity is kept");
    }

    [Fact] // AIB-07
    public async Task A_product_the_catalogue_does_not_hold_is_dropped_and_reported()
    {
        // The failure that matters: a model naming a product that does not exist. Dropping it silently
        // would show a candidate that is missing a line the customer asked for.
        var catalog = new TestCatalog()
            .Add("MON0001", 350_000m)
            .Add("DSK0001", 800_000m);

        var suggestion = await SuggestAsync(
            new FakeAgent().Answering(Ok(Option("low", ("Monitor", "MON0001", 1), ("Desk", "INVENTED", 1)))),
            catalog);

        suggestion.DroppedSkus.Should().Equal("INVENTED");
        suggestion.Options.Single().Lines.Select(line => line.Sku).Should().Equal("MON0001");
    }

    [Fact] // AIB-08
    public async Task Fewer_valid_candidates_is_stated_and_never_padded()
    {
        var catalog = new TestCatalog().Add("MON0001", 350_000m);

        var suggestion = await SuggestAsync(
            new FakeAgent().Answering(Ok(
                Option("low", ("Monitor", "MON0001", 1)),
                Option("middle", ("Monitor", "GONE", 1)))),
            catalog);

        // One candidate, not three and not two: the middle one had nothing left in it, and a candidate
        // with no products is not a cheaper candidate.
        suggestion.Options.Should().ContainSingle().Which.Tier.Should().Be("low");
        suggestion.Status.Should().Be(SuggestionStatus.Ok);
    }

    [Fact] // AIB-09
    public async Task An_agent_that_is_not_configured_is_unavailable()
    {
        var agent = new FakeAgent().NotConfigured();

        var suggestion = await SuggestAsync(agent, new TestCatalog());

        suggestion.Status.Should().Be(SuggestionStatus.Unavailable);
        suggestion.Options.Should().BeEmpty();
        agent.Asked.Should().Be(0, "a deployment with no agent should not be asked to reach one");
    }

    [Fact] // AIB-10
    public async Task An_agent_that_cannot_be_reached_is_unavailable_rather_than_refused()
    {
        // The two must not arrive as the same thing: the page says something different for "that is not
        // a workspace request" than for "the service is down", and only one of them invites a retry.
        var unreachable = await SuggestAsync(new FakeAgent().Failing(), new TestCatalog());
        var refused = await SuggestAsync(
            new FakeAgent().Answering(new AgentSuggestion(SuggestionStatus.Rejected, "not_workspace_request", [], [])),
            new TestCatalog());

        unreachable.Status.Should().Be(SuggestionStatus.Unavailable);
        refused.Status.Should().Be(SuggestionStatus.Rejected);
        refused.Code.Should().Be("not_workspace_request");
        unreachable.Status.Should().NotBe(refused.Status);
    }

    /// <summary>The answer at the end of the stream, which is what these tests are about.</summary>
    private static async Task<WorkspaceSuggestion> SuggestAsync(IAgentSuggestions agent, IProductCatalog catalog)
    {
        WorkspaceSuggestion? outcome = null;

        var updates = new SuggestWorkspaceOptions(agent, catalog)
            .SuggestAsync(new WorkspaceSuggestionRequest("a desk with two monitors"));

        await foreach (var update in updates)
        {
            if (update.Outcome is { } answer)
            {
                outcome = answer;
            }
        }

        return outcome!;
    }

    [Fact] // AIB-11
    public async Task The_stages_arrive_before_the_answer_and_in_order()
    {
        var updates = new List<SuggestionUpdate>();

        await foreach (var update in new SuggestWorkspaceOptions(
            new FakeAgent().Answering(Ok(Option("low", ("Monitor", "MON0001", 1)))),
            new TestCatalog().Add("MON0001", 350_000m))
            .SuggestAsync(new WorkspaceSuggestionRequest("a desk")))
        {
            updates.Add(update);
        }

        updates.Select(update => update.Kind).Should().Equal("stage", "result");
        updates[0].Stage.Should().Be("verifying");
        updates[0].Outcome.Should().BeNull("a stage is not an answer");
        updates[^1].Outcome.Should().NotBeNull();
    }

    private static AgentSuggestion Ok(params AgentSuggestionOption[] options)
        => new(SuggestionStatus.Ok, Code: null, options, Findings: []);

    private static AgentSuggestionOption Option(string tier, params (string Slot, string Sku, int Quantity)[] lines)
        => new(
            tier,
            [.. lines.Select(line => new AgentSuggestionLine(line.Slot, line.Sku, line.Quantity))],
            ["slot:Monitor"],
            [],
            []);

    /// <summary>The agent, said out loud instead of reached. No mocking library is used in this project.</summary>
    private sealed class FakeAgent : IAgentSuggestions
    {
        private AgentSuggestion _answer = new(SuggestionStatus.Ok, Code: null, Options: [], Findings: []);

        private bool _configured = true;
        private bool _failing;

        public int Asked { get; private set; }

        public bool IsConfigured => _configured;

        public FakeAgent Answering(AgentSuggestion answer)
        {
            _answer = answer;

            return this;
        }

        public FakeAgent NotConfigured()
        {
            _configured = false;

            return this;
        }

        public FakeAgent Failing()
        {
            _failing = true;

            return this;
        }

        public async IAsyncEnumerable<AgentSuggestionMessage> AskAsync(
            string query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Asked++;

            if (_failing)
            {
                // Thrown from inside the stream, which is where a real one fails: a request that cannot
                // be reached fails while it is being read, not when it is asked for.
                throw new HttpRequestException("the agent is not answering");
            }

            yield return AgentSuggestionMessage.StageEvent("verifying", 1);

            await Task.Yield();

            yield return AgentSuggestionMessage.Answer(_answer);
        }
    }
}
