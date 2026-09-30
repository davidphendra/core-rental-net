using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Rentals.Application;
using CoreRentalNet.Modules.Rentals.Application.Commands.Checkout;
using CoreRentalNet.Modules.Rentals.Application.Services;
using CoreRentalNet.Modules.Rentals.Application.Commands.PlaceOrder;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Composition;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Conversion;
using Xunit;
using CoreRentalNet.Modules.Rentals.Domain.Invoices;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;
using CoreRentalNet.Modules.Rentals.Domain.Rentals;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

public sealed class CheckoutTests
{
    private const string DraftToken = "draft-token-for-tests";

    private sealed class FakeConverter : IConvertWorkspaceToOrder
    {
        public FakeConverter(WorkspaceConversion conversion) => Conversion = conversion;

        public WorkspaceConversion Conversion { get; set; }

        public bool ConvertCalled { get; private set; }

        public bool Described { get; private set; }

        public Task<WorkspaceConversion> DescribeAsync(string rawDraftToken, CancellationToken cancellationToken = default)
        {
            Described = true;
            return Task.FromResult(Conversion);
        }

        public Task<WorkspaceConversion> ConvertAsync(string rawDraftToken, CancellationToken cancellationToken = default)
        {
            ConvertCalled = true;

            if (Conversion.WasAlreadyConverted)
            {
                return Task.FromResult(Conversion);
            }

            Conversion = Conversion with { WasAlreadyConverted = true };
            return Task.FromResult(Conversion with { WasAlreadyConverted = false });
        }
    }

    private sealed class FakePrices : IProductCatalogService
    {
        private readonly Dictionary<string, ProductView> views = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<ProductView> All => views.Values.ToArray();

        public FakePrices Add(string sku, decimal price, string? name = null)
        {
            views[sku] = new ProductView(
                sku.ToUpperInvariant(), name ?? $"Product {sku}", CatalogCategory.Accessory,
                CatalogSubCategory.Monitor, new Money(price, Currencies.Idr), "A description.",
                new CatalogMetadata([], new Dictionary<string, string>(), [], []),
                "/images/x.svg", true, false);
            return this;
        }

        public FakePrices Remove(string sku)
        {
            views.Remove(sku);
            return this;
        }

        public ProductView? Find(string sku) => views.TryGetValue(sku, out var view) ? view : null;

        public IReadOnlyList<ProductView> ByCategory(CatalogCategory category)
            => views.Values.Where(view => view.Category == category).ToArray();

        public IReadOnlyList<ProductView> BySubCategory(CatalogSubCategory subCategory)
            => views.Values.Where(view => view.SubCategory == subCategory).ToArray();

        public IReadOnlyList<ProductView> Featured()
            => views.Values.Where(view => view.IsFeatured).ToArray();

        public IReadOnlyList<ProductView> Search(
            CatalogCategory? category,
            CatalogSubCategory? subCategory,
            decimal? maximumMonthlyAmount = null)
            => views.Values.ToArray();
    }

    private sealed class FakePlaceOrder : IPlaceOrder
    {
        public PlaceOrderRequest? LastRequest { get; private set; }

        public int Calls { get; private set; }

        public Task<PlaceOrderResult> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastRequest = request;

            return Task.FromResult(new PlaceOrderResult(
                Guid.NewGuid(), "CR-2026-0001", "raw-order-token", "INV-2026-0001",
                new Money(1_150_000m, Currencies.Idr), new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 12)));
        }
    }

    private sealed class Fixture
    {
        public Fixture(WorkspaceConversion? conversion = null, string? address = "Villa Lotus, Canggu")
        {
            Conversion = conversion ?? new WorkspaceConversion(
                WorkspaceId,
                [new WorkspaceCompositionLine("CHA449AGLBB0", 1)],
                address,
                WasAlreadyConverted: false);

            Prices = new FakePrices().Add("CHA449AGLBB0", 400_000m, "Seminyak Lounge");
            Converter = new FakeConverter(Conversion);
            PlaceOrder = new FakePlaceOrder();
            Rentals = new InMemoryRentalRepository();
            CommandHandler = new CheckoutCommandHandler(
                Converter, Prices, Rentals, PlaceOrder, new CheckoutConfirmation(RentalsTestGraph.Money, RentalsTestGraph.Lifecycle, RentalsTestGraph.Deliveries));
        }

        public static Guid WorkspaceId { get; } = Guid.NewGuid();

        public WorkspaceConversion Conversion { get; }

        public FakePrices Prices { get; }

        public FakeConverter Converter { get; }

        public FakePlaceOrder PlaceOrder { get; }

        public InMemoryRentalRepository Rentals { get; }

        public CheckoutCommandHandler CommandHandler { get; }

        public static CheckoutCommand Command(string? confirmation = "this is a demo")
            => new(DraftToken, confirmation);
    }

    [Fact] // CO-06, CO-07, CO-14
    public async Task A_draft_with_an_item_and_an_address_becomes_an_order()
    {
        var fixture = new Fixture();

        var result = await fixture.CommandHandler.CheckoutAsync(Fixture.Command());

        result.RentalNumber.Should().Be("CR-2026-0001");
        result.InvoiceNumber.Should().Be("INV-2026-0001");
        result.RawAccessToken.Should().NotBeNullOrEmpty();
        result.WasAlreadyPlaced.Should().BeFalse();
        fixture.Converter.ConvertCalled.Should().BeTrue();
        fixture.PlaceOrder.Calls.Should().Be(1);
    }

    [Fact] // CO-04
    public async Task A_wrong_phrase_stops_everything_before_anything_is_read_or_written()
    {
        var fixture = new Fixture();

        var action = async () => await fixture.CommandHandler.CheckoutAsync(Fixture.Command("yes please"));

        await action.Should().ThrowAsync<DomainRuleViolationException>();
        fixture.Converter.Described.Should().BeFalse("the gate is checked first");
        fixture.Converter.ConvertCalled.Should().BeFalse();
        fixture.PlaceOrder.Calls.Should().Be(0);
    }

    [Fact] // CO-12
    public async Task An_empty_workspace_cannot_be_rented()
    {
        var fixture = new Fixture(new WorkspaceConversion(Fixture.WorkspaceId, [], "Villa Lotus", false));

        var action = async () => await fixture.CommandHandler.CheckoutAsync(Fixture.Command());

        await action.Should().ThrowAsync<DomainRuleViolationException>().WithMessage("*at least one item*");
        fixture.Converter.ConvertCalled.Should().BeFalse();
    }

    [Theory] // ADDR-01
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Vila")]
    public async Task A_missing_or_too_short_address_cannot_be_rented(string? address)
    {
        var fixture = new Fixture(address: address);

        var action = async () => await fixture.CommandHandler.CheckoutAsync(Fixture.Command());

        await action.Should().ThrowAsync<DomainRuleViolationException>().WithMessage("*delivery address*");
        fixture.Converter.ConvertCalled.Should().BeFalse();
    }

    [Fact] // WS-13
    public async Task An_item_that_left_the_catalog_is_refused_and_the_draft_stays_usable()
    {
        var fixture = new Fixture();
        fixture.Prices.Remove("CHA449AGLBB0");

        var action = async () => await fixture.CommandHandler.CheckoutAsync(Fixture.Command());

        await action.Should().ThrowAsync<DomainRuleViolationException>().WithMessage("*CHA449AGLBB0*no longer in the catalog*");
        fixture.Converter.ConvertCalled.Should().BeFalse("nothing may be made terminal until the order is ready");
        fixture.PlaceOrder.Calls.Should().Be(0);
    }

    [Fact] // CO-11
    public async Task Checking_out_a_second_time_returns_the_first_order()
    {
        var fixture = new Fixture();
        fixture.Rentals.Seed(new Rental
        {
            Id = RentalId.New(),
            WorkspaceId = Fixture.WorkspaceId,
            Number = RentalNumber.Of(2026, 1),
            AccessTokenHash = new OpaqueTokenService().HashOf("raw"),
            DeliveryAddress = "Villa Lotus, Canggu",
            DeliveryFee = new Money(750_000m, Currencies.Idr),
            PlacedOn = new DateOnly(2026, 1, 10),
            AnchorDate = new DateOnly(2026, 1, 10),
            Status = RentalStatus.Placed,
            Version = 1,
            Lines = [new RentalLine { Sku = "CHA449AGLBB0", Name = "Seminyak Lounge", Quantity = 1, UnitMonthlyPrice = new Money(400_000m, Currencies.Idr) }],
        });

        var result = await fixture.CommandHandler.CheckoutAsync(Fixture.Command());

        result.WasAlreadyPlaced.Should().BeTrue();
        result.RentalNumber.Should().Be("CR-2026-0001");
        fixture.PlaceOrder.Calls.Should().Be(0, "a second order must never be placed");
    }

    [Fact] // CO-11
    public async Task Losing_a_race_to_another_attempt_still_returns_an_order_rather_than_a_second_one()
    {
        // The other attempt finished between this one checking and this one converting, so the
        // first lookup misses, the conversion reports it was already done, and the second lookup
        // finds the winner's order.
        var winner = new Rental
        {
            Id = RentalId.New(),
            WorkspaceId = Fixture.WorkspaceId,
            Number = RentalNumber.Of(2026, 1),
            AccessTokenHash = new OpaqueTokenService().HashOf("raw"),
            DeliveryAddress = "Villa Lotus, Canggu",
            DeliveryFee = new Money(750_000m, Currencies.Idr),
            PlacedOn = new DateOnly(2026, 1, 10),
            AnchorDate = new DateOnly(2026, 1, 10),
            Status = RentalStatus.Placed,
            Version = 1,
            Lines = [new RentalLine { Sku = "CHA449AGLBB0", Name = "Seminyak Lounge", Quantity = 1, UnitMonthlyPrice = new Money(400_000m, Currencies.Idr) }],
        };

        var rentals = new RacingRentalRepository(winner);
        var converter = new RacingConverter(
            new WorkspaceConversion(Fixture.WorkspaceId, [new WorkspaceCompositionLine("CHA449AGLBB0", 1)], "Villa Lotus, Canggu", false),
            new WorkspaceConversion(Fixture.WorkspaceId, [new WorkspaceCompositionLine("CHA449AGLBB0", 1)], "Villa Lotus, Canggu", true));
        var placeOrder = new FakePlaceOrder();

        var service = new CheckoutCommandHandler(
            converter, new FakePrices().Add("CHA449AGLBB0", 400_000m), rentals, placeOrder,
            new CheckoutConfirmation(RentalsTestGraph.Money, RentalsTestGraph.Lifecycle, RentalsTestGraph.Deliveries));

        var result = await service.CheckoutAsync(Fixture.Command());

        result.WasAlreadyPlaced.Should().BeTrue();
        result.RentalNumber.Should().Be("CR-2026-0001");
        placeOrder.Calls.Should().Be(0, "the winner's order is returned, not a duplicate");
        converter.ConvertCalled.Should().BeTrue();
    }

    /// <summary>Misses on the first lookup, as if the winner committed its order a moment later.</summary>
    private sealed class RacingRentalRepository(Rental winner) : IRentalRepository
    {
        private int lookups;

        public Task<Rental?> FindByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
            => Task.FromResult(++lookups == 1 ? null : winner);

        public Task<Rental?> FindByTokenAsync(AccessToken token, CancellationToken cancellationToken = default)
            => Task.FromResult<Rental?>(null);

        public Task<Rental?> FindByIdAsync(RentalId id, CancellationToken cancellationToken = default)
            => Task.FromResult<Rental?>(null);

        public Task<IReadOnlyList<Rental>> ListSchedulableAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Rental>>([]);

        public Task AddAsync(Rental rental, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RacingConverter(WorkspaceConversion described, WorkspaceConversion converted) : IConvertWorkspaceToOrder
    {
        public bool ConvertCalled { get; private set; }

        public Task<WorkspaceConversion> DescribeAsync(string rawDraftToken, CancellationToken cancellationToken = default)
            => Task.FromResult(described);

        public Task<WorkspaceConversion> ConvertAsync(string rawDraftToken, CancellationToken cancellationToken = default)
        {
            ConvertCalled = true;
            return Task.FromResult(converted);
        }
    }

    [Fact] // CO-13
    public async Task Prices_come_from_the_catalog_and_not_from_the_draft()
    {
        var fixture = new Fixture();
        fixture.Prices.Add("CHA449AGLBB0", 999_000m, "Seminyak Lounge");

        await fixture.CommandHandler.CheckoutAsync(Fixture.Command());

        fixture.PlaceOrder.LastRequest!.Lines.Should().ContainSingle();
        fixture.PlaceOrder.LastRequest.Lines[0].UnitMonthlyPrice.Amount.Should().Be(
            999_000m, "the catalog is the only source of an amount");
        fixture.PlaceOrder.LastRequest.Lines[0].Name.Should().Be("Seminyak Lounge");
    }

    [Fact]
    public async Task The_order_is_told_which_workspace_it_came_from()
    {
        var fixture = new Fixture();

        await fixture.CommandHandler.CheckoutAsync(Fixture.Command());

        fixture.PlaceOrder.LastRequest!.WorkspaceId.Should().Be(Fixture.WorkspaceId);
        fixture.PlaceOrder.LastRequest.DeliveryAddress.Should().Be("Villa Lotus, Canggu");
    }

    [Fact]
    public async Task An_unknown_draft_is_reported_as_not_found()
    {
        var converter = new ThrowingConverter();

        var service = new CheckoutCommandHandler(
            converter, new FakePrices(), new InMemoryRentalRepository(), new FakePlaceOrder(),
            new CheckoutConfirmation(RentalsTestGraph.Money, RentalsTestGraph.Lifecycle, RentalsTestGraph.Deliveries));
        var action = async () => await service.CheckoutAsync(Fixture.Command());

        await action.Should().ThrowAsync<NotFoundException>();
    }

    private sealed class ThrowingConverter : IConvertWorkspaceToOrder
    {
        public Task<WorkspaceConversion> DescribeAsync(string rawDraftToken, CancellationToken cancellationToken = default)
            => throw new NotFoundException("This browser has no workspace yet.");

        public Task<WorkspaceConversion> ConvertAsync(string rawDraftToken, CancellationToken cancellationToken = default)
            => throw new NotFoundException("This browser has no workspace yet.");
    }
}
