using System.Globalization;
using AwesomeAssertions;
using CoreRentalNet.Modules.Discovery.Application.Selection;
using CoreRentalNet.Modules.Discovery.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.Selection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The selection signal's write path, against a real SQLite file.
/// </summary>
/// <remarks>
/// Two counters and the clock that moves them. What is proved here is that a customer's ACTUAL choice is what is
/// recorded — once per product, whichever slot it sat in — and that the counters decay together, because the
/// score they will express is a ratio and a ratio of two counters that moved differently stops meaning anything.
/// </remarks>
public sealed class SelectionSignalTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-09-19T00:00:00Z", CultureInfo.InvariantCulture);

    private static (SelectionCredit Credit, FakeTimeProvider Clock) Signal(
        DiscoveryContext context,
        TimeSpan? halfLife = null)
    {
        var clock = new FakeTimeProvider(Start);

        return (new SelectionCredit(context, new SelectionSettings(halfLife ?? SelectionSettings.DefaultHalfLife), clock), clock);
    }

    private static async Task<Dictionary<string, double>> ChosenAsync(DiscoveryContext context)
        => await context.Selections.ToDictionaryAsync(row => row.Sku, row => row.DecayedChosen);

    [Fact] // SCR-23
    public async Task An_applied_candidate_credits_exactly_the_skus_it_contains()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var (credit, _) = Signal(context);

        var first = await credit.CreditAsync(["DSKB08XN4JDR", "CHA449AGLBB0"], CancellationToken.None);
        await credit.CreditAsync(["CHA449AGLBB0"], CancellationToken.None);

        first.Should().Be(2);

        var chosen = await ChosenAsync(context);
        chosen.Should().HaveCount(2, "only the products the candidates named are recorded");
        chosen["DSKB08XN4JDR"].Should().Be(1);
        chosen["CHA449AGLBB0"].Should().Be(2);
    }

    [Fact] // SCR-23
    public async Task The_same_product_named_twice_in_one_candidate_is_credited_once()
    {
        // A candidate may legitimately name one product in two slots - three of the same monitor is still one
        // monitor - so crediting it twice would make a repeated product look like two decisions.
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var (credit, _) = Signal(context);

        var credited = await credit.CreditAsync(["MONJVAP81NPQ", "MONJVAP81NPQ"], CancellationToken.None);

        credited.Should().Be(1);
        (await ChosenAsync(context))["MONJVAP81NPQ"].Should().Be(1);
    }

    [Fact] // SCR-23
    public async Task A_credit_leaves_the_offered_count_alone()
    {
        // The denominator belongs to the retrieval path and is written there. If crediting touched it, a product
        // chosen once would look as though it had been offered once - and its score would read as a perfect
        // hundred per cent, which is exactly the drift the ratio is meant to prevent.
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var (credit, _) = Signal(context);

        await credit.CreditAsync(["DSKB08XN4JDR"], CancellationToken.None);

        (await context.Selections.SingleAsync()).DecayedOffered.Should().Be(0);
    }

    [Fact] // SCR-23
    public async Task Crediting_nothing_writes_no_row()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var (credit, _) = Signal(context);

        var credited = await credit.CreditAsync([], CancellationToken.None);

        credited.Should().Be(0);
        (await context.Selections.CountAsync()).Should().Be(0);
    }

    [Fact] // SCR-24
    public async Task An_offer_and_a_credit_move_the_same_row_and_the_same_ratio()
    {
        // Both writes go through the same row logic, so a product offered four times and chosen once scores a
        // quarter - and neither write can decay the row its own way and leave the other disagreeing about what
        // the ratio means.
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var (credit, clock) = Signal(context);
        var offers = new SelectionOffers(context, SelectionSettings.Default, clock);

        for (var run = 0; run < 4; run++)
        {
            await offers.OfferAsync(["DSKB08XN4JDR", "CHA449AGLBB0"], CancellationToken.None);
        }

        await credit.CreditAsync(["DSKB08XN4JDR"], CancellationToken.None);

        var chosen = await context.Selections.SingleAsync(row => row.Sku == "DSKB08XN4JDR");
        var never = await context.Selections.SingleAsync(row => row.Sku == "CHA449AGLBB0");

        chosen.DecayedOffered.Should().Be(4);
        chosen.DecayedChosen.Should().Be(1);
        never.DecayedOffered.Should().Be(4);
        never.DecayedChosen.Should().Be(0);
    }

    [Fact] // SCR-24
    public async Task An_offer_records_each_product_once_however_it_was_named()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var (_, clock) = Signal(context);
        var offers = new SelectionOffers(context, SelectionSettings.Default, clock);

        var offered = await offers.OfferAsync(["MONJVAP81NPQ", "MONJVAP81NPQ"], CancellationToken.None);

        offered.Should().Be(1);
        (await context.Selections.SingleAsync()).DecayedOffered.Should().Be(1);
    }

    [Fact] // SCR-25, through the write path rather than through the arithmetic
    public async Task What_was_chosen_a_half_life_ago_is_worth_half_as_much()
    {
        // The first credit is worth one. A half-life later it is worth a half, and the second credit adds one on
        // top of that - so the row reads 1.5 rather than 2. That ordering is the whole of the decay rule: adding
        // before decaying would count today's choice as though it were as old as the row.
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var (credit, clock) = Signal(context);

        await credit.CreditAsync(["DSKB08XN4JDR"], CancellationToken.None);

        clock.Advance(SelectionSettings.DefaultHalfLife);
        await credit.CreditAsync(["DSKB08XN4JDR"], CancellationToken.None);

        (await context.Selections.SingleAsync()).DecayedChosen.Should().BeApproximately(1.5, 0.000001);
    }

    [Fact] // SCR-25
    public async Task A_row_touched_again_later_keeps_decaying_from_where_it_was()
    {
        // Each write decays from the row's OWN last update, not from a fixed origin, so two months of silence and
        // two months of monthly credits do not decay to the same place.
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedDiscoveryContextAsync();
        var (credit, clock) = Signal(context, halfLife: TimeSpan.FromDays(30));

        await credit.CreditAsync(["DSKB08XN4JDR"], CancellationToken.None);
        clock.Advance(TimeSpan.FromDays(30));
        await credit.CreditAsync(["DSKB08XN4JDR"], CancellationToken.None);
        clock.Advance(TimeSpan.FromDays(30));
        await credit.CreditAsync(["DSKB08XN4JDR"], CancellationToken.None);

        // 1 -> 0.5+1 = 1.5 -> 0.75+1 = 1.75
        (await context.Selections.SingleAsync()).DecayedChosen.Should().BeApproximately(1.75, 0.000001);
    }
}
