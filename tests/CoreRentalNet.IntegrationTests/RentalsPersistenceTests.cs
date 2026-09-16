using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Rentals.Application;
using CoreRentalNet.Modules.Rentals.Application.Orders;
using CoreRentalNet.Modules.Rentals.Application.Queries.GetRentalByToken;
using CoreRentalNet.Modules.Rentals.Application.Queries.GetInvoicesByToken;
using CoreRentalNet.Modules.Rentals.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Numbering;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.IntegrationTests;

public sealed class RentalsPersistenceTests
{
    private static FakeTimeProvider Clock() => new(new DateTimeOffset(2026, 1, 10, 6, 0, 0, TimeSpan.Zero));

    private static PlaceOrderRequest Request(params OrderLineRequest[] lines)
        => new(
            Guid.NewGuid(),
            lines.Length == 0
                ? [new OrderLineRequest("CHA449AGLBB0", "Seminyak Lounge", 1, new Money(400_000m, Currencies.Idr))]
                : lines,
            "Villa Lotus, Canggu");

    private static PlaceOrderService Service(RentalsContext context, FakeTimeProvider clock)
        => new(
            new RentalRepository(context),
            new InvoiceRepository(context),
            RentalsGraph.Tokens, RentalsGraph.Invoicing,
            RentalsGraph.Lifecycle,
            RentalsGraph.Deliveries,
            new SqliteNumberSequence(context),
            new RentalsUnitOfWork(context),
            new RentalsSettings(new Money(750_000m, Currencies.Idr)),
            clock);

    [Fact] // CO-06, CO-07
    public async Task An_order_and_its_first_invoice_survive_a_round_trip_through_the_file()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();

        await using (var write = await database.CreateMigratedRentalsContextAsync())
        {
            var result = await Service(write, clock).PlaceAsync(Request());
            result.RentalNumber.Should().Be("CR-2026-0001");

            var readBack = await new GetRentalByTokenHandler(new RentalRepository(write), RentalsGraph.Tokens, RentalsGraph.Money, RentalsGraph.Lifecycle, clock)
                .HandleAsync(new GetRentalByTokenQuery(result.RawAccessToken));

            readBack.Should().NotBeNull();
            readBack!.MonthlyTotal.Amount.Should().Be(400_000m);
        }

        await using var read = await database.CreateMigratedRentalsContextAsync();
        var rentals = await read.Rentals.ToArrayAsync();
        var invoices = await read.Invoices.ToArrayAsync();

        rentals.Should().ContainSingle();
        invoices.Should().ContainSingle();
        rentals[0].Lines.Should().ContainSingle();
        invoices[0].Lines.Should().ContainSingle();
        invoices[0].Total.Amount.Should().Be(1_150_000m);
        invoices[0].Status.Should().Be(InvoiceStatus.Paid);
        rentals[0].Status.Should().Be(RentalStatus.Paid);
    }

    [Fact] // CO-14
    public async Task Numbers_are_sequential_across_separate_sessions()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();

        await using (var first = await database.CreateMigratedRentalsContextAsync())
        {
            await Service(first, clock).PlaceAsync(Request());
        }

        await using (var second = await database.CreateMigratedRentalsContextAsync())
        {
            var result = await Service(second, clock).PlaceAsync(Request());

            result.RentalNumber.Should().Be("CR-2026-0002", "the counter is in the database, not in memory");
            result.InvoiceNumber.Should().Be("INV-2026-0002");
        }
    }

    [Fact] // CO-14
    public async Task Reserving_numbers_concurrently_never_hands_out_a_duplicate()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedRentalsContextAsync();

        var sequence = new SqliteNumberSequence(context);

        var reserved = await Task.WhenAll(
            Enumerable.Range(0, 12).Select(_ => sequence.ReserveNextAsync(SequenceKind.Rental, 2026)));

        reserved.Should().OnlyHaveUniqueItems();
        reserved.OrderBy(value => value).Should().Equal(Enumerable.Range(1, 12));
    }

    [Fact]
    public async Task Numbers_are_counted_per_year()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedRentalsContextAsync();
        var sequence = new SqliteNumberSequence(context);

        (await sequence.ReserveNextAsync(SequenceKind.Rental, 2026)).Should().Be(1);
        (await sequence.ReserveNextAsync(SequenceKind.Rental, 2026)).Should().Be(2);
        (await sequence.ReserveNextAsync(SequenceKind.Rental, 2027)).Should().Be(1, "a new year starts again at one");
        (await sequence.ReserveNextAsync(SequenceKind.Invoice, 2026)).Should().Be(1, "orders and invoices count separately");
    }

    [Fact] // SEC-02
    public async Task Only_the_access_token_hash_is_written_to_the_file()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        await using var context = await database.CreateMigratedRentalsContextAsync();

        var result = await Service(context, clock).PlaceAsync(Request());

        var hashes = await context.Database
            .SqlQueryRaw<string>("SELECT AccessTokenHash AS Value FROM Rentals_Rental")
            .ToArrayAsync();

        hashes.Should().ContainSingle();
        hashes[0].Should().Be(new OpaqueTokenService().HashOf(result.RawAccessToken));
        hashes[0].Should().NotContain(result.RawAccessToken);
    }

    [Fact] // ARC-03
    public async Task Every_rentals_table_belongs_to_this_module()
    {
        await using var database = new SqliteTestDatabase();
        await using var context = await database.CreateMigratedRentalsContextAsync();

        var allTables = await context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToArrayAsync();

        var tables = allTables
            .Where(table => !table.StartsWith("sqlite_", StringComparison.Ordinal))
            .Where(table => !table.StartsWith("__EF", StringComparison.Ordinal))
            .ToArray();

        tables.Should().NotBeEmpty();
        tables.Should().OnlyContain(table => table.StartsWith("Rentals_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Each_module_keeps_its_own_migration_history_in_the_shared_file()
    {
        await using var database = new SqliteTestDatabase();

        await using (var workspace = await database.CreateMigratedContextAsync())
        {
            workspace.Database.GetAppliedMigrations().Should().NotBeEmpty();
        }

        await using (var rentals = await database.CreateMigratedRentalsContextAsync())
        {
            rentals.Database.GetAppliedMigrations().Should().NotBeEmpty();
        }

        await using var check = await database.CreateMigratedRentalsContextAsync();
        var history = await check.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table' AND name LIKE '%MigrationsHistory'")
            .ToArrayAsync();

        history.Should().Contain("Workspace_MigrationsHistory").And.Contain("Rentals_MigrationsHistory");
        history.Should().NotContain("__EFMigrationsHistory");
    }

    [Fact]
    public async Task Money_survives_the_conversion_in_both_directions()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        await using var context = await database.CreateMigratedRentalsContextAsync();

        await Service(context, clock).PlaceAsync(Request(
            new OrderLineRequest("MONJVAP81NPQ", "Batu Bolong 27\" 4K", 3, new Money(333_333.33m, Currencies.Idr))));

        context.ChangeTracker.Clear();

        var stored = await context.Rentals.SingleAsync();

        stored.Lines[0].UnitMonthlyPrice.Amount.Should().Be(333_333.33m);
        stored.Lines[0].UnitMonthlyPrice.Currency.Should().Be(Currencies.Idr);
        RentalsGraph.Money.Round(RentalsGraph.Money.Times(stored.Lines[0].UnitMonthlyPrice, stored.Lines[0].Quantity)).Amount.Should().Be(999_999.99m);
        RentalsGraph.Lifecycle.MonthlyTotal(stored).Amount.Should().Be(999_999.99m);
        stored.DeliveryFee.Amount.Should().Be(750_000m);
    }

    [Fact] // ORD-01
    public async Task An_order_is_readable_by_its_token_after_a_restart()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        string rawToken;

        await using (var first = await database.CreateMigratedRentalsContextAsync())
        {
            rawToken = (await Service(first, clock).PlaceAsync(Request())).RawAccessToken;
        }

        await using var second = await database.CreateMigratedRentalsContextAsync();
        var view = await new GetRentalByTokenHandler(new RentalRepository(second), RentalsGraph.Tokens, RentalsGraph.Money, RentalsGraph.Lifecycle, clock)
            .HandleAsync(new GetRentalByTokenQuery(rawToken));

        view.Should().NotBeNull();
        view!.Lines.Should().ContainSingle();
        view.DeliveryAddress.Should().Be("Villa Lotus, Canggu");
        view.CurrentPeriodStart.Should().Be(new DateOnly(2026, 1, 10));
    }

    [Fact] // ORD-02, SEC-06
    public async Task A_guessed_token_finds_nothing_in_the_real_database()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        await using var context = await database.CreateMigratedRentalsContextAsync();
        await Service(context, clock).PlaceAsync(Request());

        var rentals = new RentalRepository(context);
        var invoices = new InvoiceRepository(context);

        (await rentals.FindByTokenAsync(new AccessToken(new OpaqueTokenService().HashOf("guessed")))).Should().BeNull();
        (await new GetRentalByTokenHandler(rentals, RentalsGraph.Tokens, RentalsGraph.Money, RentalsGraph.Lifecycle, clock).HandleAsync(new GetRentalByTokenQuery("guessed"))).Should().BeNull();
        (await new GetInvoicesByTokenHandler(rentals, invoices, RentalsGraph.Tokens, RentalsGraph.Invoicing).HandleAsync(new GetInvoicesByTokenQuery("guessed"))).Should().BeEmpty();
    }

    [Fact]
    public async Task Two_invoices_for_the_same_period_cannot_both_exist()
    {
        await using var database = new SqliteTestDatabase();
        var clock = Clock();
        await using var context = await database.CreateMigratedRentalsContextAsync();

        var result = await Service(context, clock).PlaceAsync(Request());
        var rental = await new RentalRepository(context).FindByIdAsync(RentalId.From(result.RentalId));

        var duplicate = RentalsGraph.Invoicing.IssueFor(
            InvoiceId.New(),
            InvoiceNumber.Of(2026, 99),
            rental!,
            periodIndex: 0,
            taxRate: 0m,
            new DateOnly(2026, 1, 10));

        await new InvoiceRepository(context).AddAsync(duplicate);

        var action = async () => await context.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>(
            "an invoice is issued once per period per order");
    }
}
