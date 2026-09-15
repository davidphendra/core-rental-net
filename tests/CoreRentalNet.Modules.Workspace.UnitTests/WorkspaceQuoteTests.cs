using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Queries.Services;
using CoreRentalNet.Modules.Workspace.Application.Queries.Views;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Rules;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Services;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;
using CoreRentalNet.BuildingBlocks.Application;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

public sealed class WorkspaceQuoteTests
{
    private static readonly ISlotRuleProvider Rules = new SlotRuleProvider();
    private static readonly IMoneyService Money = new MoneyService();
    private static readonly IWorkspaceService Workspaces = new WorkspaceService(Rules);
    private static readonly IWorkspaceQueryService Queries = new WorkspaceQueryService(Rules);

    private static Domain.Workspace NewDraft() => new()
    {
        Id = WorkspaceId.New(),
        DraftTokenHash = new OpaqueTokenService().HashOf("raw"),
        State = DraftState.Draft,
        Version = 1,
        Assignments = [],
    };

    private static WorkspaceQuote Quote(Domain.Workspace workspace, TestCatalog catalog)
        => new WorkspaceQuoteService(Money, catalog).Quote(workspace);

    private static WorkspaceView View(Domain.Workspace workspace, TestCatalog catalog)
        => new WorkspaceViewService(Rules, new WorkspaceQuoteService(Money, catalog), Queries).Build(workspace);

    [Fact] // WS-11
    public void The_monthly_total_is_the_sum_of_price_times_quantity()
    {
        var catalog = new TestCatalog()
            .Add("CHA0001", 400000m, CatalogCategory.Chair, null)
            .Add("MON0001", 300000m);
        var workspace = NewDraft();
        Workspaces.Assign(workspace, SlotId.Chair, "CHA0001");
        Workspaces.Assign(workspace, SlotId.Monitor, "MON0001", 2);

        var quote = Quote(workspace, catalog);

        quote.MonthlySubtotal.Amount.Should().Be(1000000m);
        quote.MonthlySubtotal.Currency.Should().Be("IDR");
        quote.TotalUnits.Should().Be(3);
        quote.Lines.Should().HaveCount(2);
        quote.Lines.Single(line => line.Sku == "MON0001").LineMonthlyTotal!.Amount.Should().Be(600000m);
    }

    [Fact] // WS-11
    public void An_empty_workspace_costs_zero_rather_than_failing()
    {
        var workspace = NewDraft();

        var quote = Quote(workspace, new TestCatalog());

        quote.MonthlySubtotal.Amount.Should().Be(0m);
        quote.MonthlySubtotal.Currency.Should().Be("IDR");
        quote.IsEmpty.Should().BeTrue();
    }

    [Fact] // WS-12
    public void A_price_change_is_reflected_on_the_next_read()
    {
        var catalog = new TestCatalog().Add("CHA0001", 400000m, CatalogCategory.Chair, null);
        var workspace = NewDraft();
        Workspaces.Assign(workspace, SlotId.Chair, "CHA0001");

        Quote(workspace, catalog).MonthlySubtotal.Amount.Should().Be(400000m);

        catalog.Add("CHA0001", 450000m, CatalogCategory.Chair, null);

        Quote(workspace, catalog).MonthlySubtotal.Amount.Should().Be(450000m);
    }

    [Fact] // WS-13
    public void A_sku_the_catalog_no_longer_knows_is_reported_and_excluded_from_the_total()
    {
        var catalog = new TestCatalog()
            .Add("CHA0001", 400000m, CatalogCategory.Chair, null)
            .Add("MON0001", 300000m);
        var workspace = NewDraft();
        Workspaces.Assign(workspace, SlotId.Chair, "CHA0001");
        Workspaces.Assign(workspace, SlotId.Monitor, "MON0001", 2);

        catalog.Remove("MON0001");

        var quote = Quote(workspace, catalog);

        quote.HasUnavailableLines.Should().BeTrue();
        quote.UnavailableLines.Should().ContainSingle().Which.Sku.Should().Be("MON0001");
        quote.UnavailableLines.Single().IsAvailable.Should().BeFalse();
        quote.MonthlySubtotal.Amount.Should().Be(400000m, "an unavailable line is never priced at zero, it is excluded and flagged");
    }

    [Fact] // WS-13
    public void A_workspace_whose_only_product_disappeared_totals_zero_and_reports_the_gap()
    {
        var catalog = new TestCatalog().Add("CHA0001", 400000m, CatalogCategory.Chair, null);
        var workspace = NewDraft();
        Workspaces.Assign(workspace, SlotId.Chair, "CHA0001");

        catalog.Remove("CHA0001");

        var quote = Quote(workspace, catalog);

        quote.MonthlySubtotal.Amount.Should().Be(0m);
        quote.HasUnavailableLines.Should().BeTrue();
    }

    [Fact] // WS-11
    public void Rounding_happens_once_over_the_whole_workspace()
    {
        var catalog = new TestCatalog()
            .Add("MON0001", 100000.005m)
            .Add("MON0002", 100000.005m, name: "Second monitor");
        var workspace = NewDraft();
        Workspaces.Assign(workspace, SlotId.Monitor, "MON0001", 1);
        Workspaces.Assign(workspace, SlotId.Monitor, "MON0002", 2);

        var quote = Quote(workspace, catalog);

        quote.MonthlySubtotal.Amount.Should().Be(300000.02m);
    }

    [Fact]
    public void The_view_lists_every_slot_including_the_empty_ones()
    {
        var catalog = new TestCatalog()
            .Add("CHA0001", 400000m, CatalogCategory.Chair, null)
            .Add("DSK0001", 800000m, CatalogCategory.Desk, null);
        var workspace = NewDraft();
        Workspaces.Assign(workspace, SlotId.Chair, "CHA0001");
        Workspaces.Assign(workspace, SlotId.Desk, "DSK0001");

        var view = View(workspace, catalog);

        view.Slots.Should().HaveCount(7);
        view.Slots.Single(slot => slot.Slot == SlotId.Chair).IsFilled.Should().BeTrue();
        view.Slots.Single(slot => slot.Slot == SlotId.Plant).IsFilled.Should().BeFalse("every slot is listed, filled or not");
        view.CanCheckout.Should().BeTrue();
        view.TotalUnits.Should().Be(2);
    }

    [Fact]
    public void A_workspace_with_an_unavailable_line_cannot_check_out()
    {
        var catalog = new TestCatalog().Add("CHA0001", 400000m, CatalogCategory.Chair, null);
        var workspace = NewDraft();
        Workspaces.Assign(workspace, SlotId.Chair, "CHA0001");
        catalog.Remove("CHA0001");

        View(workspace, catalog).CanCheckout.Should().BeFalse();
    }
}
