using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.AiBuilder;
using CoreRentalNet.Modules.Discovery.Application.Selection;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// When a shortlist counts as offered, which is the whole of the signal's denominator.
/// </summary>
/// <remarks>
/// <b>The failure this prevents is a wrong denominator rather than a missing number.</b> Fourteen products
/// counted as offered on a run where the model was never reached inflates the denominator, drops every score, and
/// reads as "customers do not like anything" — a signal that looks like a finding and is a bug. So the rule is
/// proved from both sides: a run that reached the model offers once, and every way of not reaching it offers
/// nothing.
/// </remarks>
public sealed class OfferedCountTests
{
    private static readonly AgentSuggestionEvent[] Answered =
    [
        new AgentSuggestionEvent.NarrativeDelta("A calm, focused setup."),
        new AgentSuggestionEvent.Completed(Result(), "payload-hash"),
    ];

    private static AgentSuggestionResult Result()
        => new(
            AgentSuggestionStatus.Suggested,
            null,
            [
                new AgentSuggestionOption(
                    [new AgentSuggestionLine(Modules.Workspace.Domain.SlotId.Desk, "DSKB08XN4JDR", 1, "a stable surface")],
                    "A calm, focused setup."),
            ],
            null);

    [Fact] // SCR-24
    public async Task A_run_that_reached_the_model_offers_its_shortlist_once()
    {
        var offers = new RecordingOffers();

        await Run(Answered, offers: offers);

        offers.Offered.Should().ContainSingle("one run is one offer, however many events it produced");
        offers.Offered[0].Should().Equal(["DSKB08XN4JDR"]);
    }

    [Fact] // SCR-24
    public async Task A_run_whose_retrieval_failed_offers_nothing()
    {
        // There was no shortlist to offer: retrieval never produced one, so the model was never given anything.
        var offers = new RecordingOffers();

        await Run(Answered, shortlist: new UnavailableCatalogShortlist(), offers: offers);

        offers.Offered.Should().BeEmpty();
    }

    [Fact] // SCR-24
    public async Task A_run_whose_agent_was_never_reached_offers_nothing()
    {
        // The adapter yields UNAVAILABLE when it cannot build the agent, when the transport refuses, or when the
        // run times out — and a run that never reached the model showed its shortlist to nobody. This is the case
        // that made the rule "an event only a model can produce" rather than "any event at all".
        var offers = new RecordingOffers();

        await Run([new AgentSuggestionEvent.Unavailable("no agent is configured")], offers: offers);

        offers.Offered.Should().BeEmpty();
    }

    [Fact] // SCR-24
    public async Task A_run_the_model_never_answered_offers_nothing()
    {
        // No events at all: the stream ended without the model saying anything.
        var offers = new RecordingOffers();

        await Run([], offers: offers);

        offers.Offered.Should().BeEmpty();
    }

    [Fact] // SCR-24
    public async Task A_signal_that_cannot_be_written_does_not_fail_the_run()
    {
        // The answer is already arriving and a customer is waiting for it. Losing one offer is a fact to log, not
        // a reason to throw away a run that has been paid for.
        var frames = await Run(Answered, offers: new RecordingOffers(fail: true));

        frames.Should().NotContain(frame => frame.Event == "failed");
        frames.Should().Contain(frame => frame.Event == "result", "the run still finishes");
    }

    private static async Task<IReadOnlyList<SuggestionRunTests.Frame>> Run(
        AgentSuggestionEvent[] scripted,
        ICatalogShortlist? shortlist = null,
        IOfferSelection? offers = null)
        => await SuggestionRunTests.FramesAsync(scripted, shortlist, offers);
}
