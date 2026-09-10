using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Catalog.Infrastructure;
using CoreRentalNet.Modules.Catalog.Infrastructure.Loading;
using CoreRentalNet.Modules.Rentals.Application;
using CoreRentalNet.Modules.Rentals.Application.Checkout;
using CoreRentalNet.Modules.Rentals.Application.Orders;
using CoreRentalNet.Modules.Rentals.Application.Queries;
using CoreRentalNet.Modules.Rentals.Domain;
using CoreRentalNet.Modules.Rentals.Infrastructure;
using CoreRentalNet.Modules.Workspace.Application.Commands;
using CoreRentalNet.Modules.Workspace.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Domain;
using CoreRentalNet.Modules.Workspace.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The whole funnel against two real databases and the real catalog file: a draft is filled,
/// checkout turns it into an order, and the draft is left terminal.
/// </summary>
public sealed class EndToEndCheckoutTests
{
    private const string RawDraftToken = "integration-draft-token";
    private const string Chair = "CHA449AGLBB0";
    private const string Monitor = "MONJVAP81NPQ";

    private static IDefineProductPrices RealCatalog()
        => new DefineProductPrices(CatalogLoader.LoadFromFile(
            RepoRoot.Combine("src", "shared", "data", "products.json"),
            RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "wwwroot")));

    private static FakeTimeProvider Clock() => new(new DateTimeOffset(2026, 1, 10, 6, 0, 0, TimeSpan.Zero));

    private sealed record Harness(
        WorkspaceContext Workspace,
        RentalsContext Rentals,
        ICheckout Checkout,
        IConvertWorkspaceToOrder Converter,
        FakeTimeProvider Clock);

    private static async Task<Harness> ArrangeAsync(SqliteTestDatabase database, FakeTimeProvider clock)
    {
        var workspaceContext = await database.CreateMigratedContextAsync();
        var rentalsContext = await database.CreateMigratedRentalsContextAsync();

        var workspaceRepository = new WorkspaceRepository(workspaceContext);
        var rentalRepository = new RentalRepository(rentalsContext);
        var invoiceRepository = new InvoiceRepository(rentalsContext);

        var prices = RealCatalog();

        // A customer fills a workspace.
        await new StartDraftHandler(workspaceRepository, prices).HandleAsync(new StartDraft(RawDraftToken));
        // The chair slot holds one unit; the monitor slot holds up to three.
        await new AssignProductHandler(workspaceRepository, prices).HandleAsync(new AssignProduct(RawDraftToken, Chair));
        await new AssignProductHandler(workspaceRepository, prices).HandleAsync(new AssignProduct(RawDraftToken, Monitor, 2));
        await new SetDeliveryAddressHandler(workspaceRepository, prices)
            .HandleAsync(new SetDeliveryAddress(RawDraftToken, "Villa Lotus, Canggu"));

        var converter = new ConvertWorkspaceToOrder(workspaceRepository);

        var placeOrder = new PlaceOrderService(
            rentalRepository,
            invoiceRepository,
            new SqliteNumberSequence(rentalsContext),
            new RentalsUnitOfWork(rentalsContext),
            new RentalsSettings(Money.Idr(750_000m)),
            clock);

        var checkout = new CheckoutService(converter, prices, rentalRepository, placeOrder);

        return new Harness(workspaceContext, rentalsContext, checkout, converter, clock);
    }

    [Fact] // CO-01 to CO-07, CO-10, ORD-01, ORD-03
    public async Task A_draft_becomes_a_paid_order_and_the_draft_is_finished()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        var harness = await ArrangeAsync(database, clock);

        var result = await harness.Checkout.CheckoutAsync(new CheckoutCommand(RawDraftToken, "  This Is A DEMO "));

        result.WasAlreadyPlaced.Should().BeFalse();
        result.RentalNumber.Should().Be("CR-2026-0001");
        result.InvoiceNumber.Should().Be("INV-2026-0001");
        result.RawAccessToken.Should().NotBeNullOrEmpty();

        // The order: a 400,000 chair, two 300,000 monitors, and the 750,000 delivery charge.
        result.FirstInvoiceTotal.Amount.Should().Be(1_750_000m);
        result.DeliveryScheduledFor.Should().Be(new DateOnly(2026, 1, 12));

        harness.Workspace.ChangeTracker.Clear();
        var storedDraft = await new WorkspaceRepository(harness.Workspace)
            .FindByTokenAsync(DraftToken.FromRawToken(RawDraftToken));
        storedDraft!.State.Should().Be(DraftState.Converted, "the cart is finished, which is what empties it");

        harness.Rentals.ChangeTracker.Clear();
        var rental = await new RentalRepository(harness.Rentals).FindByWorkspaceIdAsync(storedDraft.Id.Value);
        rental.Should().NotBeNull();
        rental!.Status.Should().Be(RentalStatus.Paid);
        rental.MonthlyTotal.Amount.Should().Be(1_000_000m);
        rental.AccessTokenHash.Should().Be(AccessToken.HashOf(result.RawAccessToken));

        var invoices = await new InvoiceRepository(harness.Rentals).ListForRentalAsync(rental.Id);
        invoices.Should().ContainSingle();
        invoices[0].Status.Should().Be(InvoiceStatus.Paid);
        invoices[0].DeliveryFee.Amount.Should().Be(750_000m);
        invoices[0].HasTaxLine.Should().BeFalse("the tax rate is seeded to zero");
        invoices[0].Lines.Should().HaveCount(2);
        invoices[0].Lines.Sum(line => line.Quantity).Should().Be(3);

        // The confirmation the customer is sent to.
        var view = await new GetRentalByTokenHandler(new RentalRepository(harness.Rentals), clock)
            .HandleAsync(new GetRentalByToken(result.RawAccessToken));

        view.Should().NotBeNull();
        view!.Number.Should().Be(result.RentalNumber);
        view.DeliveryAddress.Should().Be("Villa Lotus, Canggu");
        view.Lines.Should().HaveCount(2);
        view.MonthlyTotal.Amount.Should().Be(1_000_000m);
    }

    [Fact] // CO-04
    public async Task A_mistyped_phrase_leaves_the_draft_untouched_and_creates_nothing()
    {
        await using var database = new SqliteTestDatabase();
        var harness = await ArrangeAsync(database, Clock());

        var action = async () => await harness.Checkout.CheckoutAsync(new CheckoutCommand(RawDraftToken, "this is a demo?"));

        await action.Should().ThrowAsync<DomainRuleViolationException>();

        harness.Workspace.ChangeTracker.Clear();
        harness.Rentals.ChangeTracker.Clear();

        var draft = await new WorkspaceRepository(harness.Workspace).FindByTokenAsync(DraftToken.FromRawToken(RawDraftToken));
        draft!.State.Should().Be(DraftState.Draft, "a failed checkout must leave a usable workspace");
        draft.Assignments.Should().HaveCount(2, "the chair and the monitors are both still there");
        draft.TotalUnits.Should().Be(3);

        (await harness.Rentals.Rentals.ToListAsync()).Should().BeEmpty();
        (await harness.Rentals.Invoices.ToListAsync()).Should().BeEmpty();
    }

    [Fact] // ADDR-01
    public async Task Checkout_without_an_address_is_refused_and_leaves_the_draft_usable()
    {
        await using var database = new SqliteTestDatabase();
        var harness = await ArrangeAsync(database, Clock());

        var action = async () => await harness.Checkout.CheckoutAsync(new CheckoutCommand(RawDraftToken, "this is a demo"));
        await action.Should().NotThrowAsync();

        // A second workspace with items but no address, which is the case under test.
        await using var other = new SqliteTestDatabase();
        var workspaceContext = await other.CreateMigratedContextAsync();
        var rentalsContext = await other.CreateMigratedRentalsContextAsync();
        var workspaceRepository = new WorkspaceRepository(workspaceContext);
        var prices = RealCatalog();

        await new StartDraftHandler(workspaceRepository, prices).HandleAsync(new StartDraft("no-address"));
        await new AssignProductHandler(workspaceRepository, prices).HandleAsync(new AssignProduct("no-address", Chair));

        var checkout = new CheckoutService(
            new ConvertWorkspaceToOrder(workspaceRepository),
            prices,
            new RentalRepository(rentalsContext),
            new PlaceOrderService(
                new RentalRepository(rentalsContext), new InvoiceRepository(rentalsContext),
                new SqliteNumberSequence(rentalsContext), new RentalsUnitOfWork(rentalsContext),
                new RentalsSettings(Money.Idr(750_000m)), Clock()));

        var refused = async () => await checkout.CheckoutAsync(new CheckoutCommand("no-address", "this is a demo"));

        await refused.Should().ThrowAsync<DomainRuleViolationException>().WithMessage("*delivery address*");

        workspaceContext.ChangeTracker.Clear();
        var draft = await new WorkspaceRepository(workspaceContext).FindByTokenAsync(DraftToken.FromRawToken("no-address"));
        draft!.State.Should().Be(DraftState.Draft);
    }

    [Fact] // CO-11
    public async Task Checking_out_twice_creates_one_order_and_returns_it_both_times()
    {
        await using var database = new SqliteTestDatabase();
        var harness = await ArrangeAsync(database, Clock());

        var first = await harness.Checkout.CheckoutAsync(new CheckoutCommand(RawDraftToken, "this is a demo"));
        var second = await harness.Checkout.CheckoutAsync(new CheckoutCommand(RawDraftToken, "this is a demo"));

        second.WasAlreadyPlaced.Should().BeTrue();
        second.RentalNumber.Should().Be(first.RentalNumber);
        second.RawAccessToken.Should().BeEmpty("the token exists once and is not recoverable");

        harness.Rentals.ChangeTracker.Clear();
        (await harness.Rentals.Rentals.ToListAsync()).Should().ContainSingle("a second order must never be placed");
        (await harness.Rentals.Invoices.ToListAsync()).Should().ContainSingle();
    }

    [Fact] // CO-12
    public async Task An_empty_workspace_cannot_be_checked_out()
    {
        await using var database = new SqliteTestDatabase();
        var workspaceContext = await database.CreateMigratedContextAsync();
        var rentalsContext = await database.CreateMigratedRentalsContextAsync();
        var workspaceRepository = new WorkspaceRepository(workspaceContext);
        var prices = RealCatalog();

        await new StartDraftHandler(workspaceRepository, prices).HandleAsync(new StartDraft("empty"));
        await new SetDeliveryAddressHandler(workspaceRepository, prices)
            .HandleAsync(new SetDeliveryAddress("empty", "Villa Lotus, Canggu"));

        var checkout = new CheckoutService(
            new ConvertWorkspaceToOrder(workspaceRepository), prices, new RentalRepository(rentalsContext),
            new PlaceOrderService(new RentalRepository(rentalsContext), new InvoiceRepository(rentalsContext),
                new SqliteNumberSequence(rentalsContext), new RentalsUnitOfWork(rentalsContext),
                new RentalsSettings(Money.Idr(750_000m)), Clock()));

        var action = async () => await checkout.CheckoutAsync(new CheckoutCommand("empty", "this is a demo"));

        await action.Should().ThrowAsync<DomainRuleViolationException>().WithMessage("*at least one item*");
        workspaceContext.ChangeTracker.Clear();
        var untouched = await new WorkspaceRepository(workspaceContext).FindByTokenAsync(DraftToken.FromRawToken("empty"));
        untouched!.State.Should().Be(DraftState.Draft);
    }
}
