using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Commands;
using CoreRentalNet.Modules.Workspace.Application.Queries;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

public sealed class WorkspaceCommandTests
{
    private static TestCatalog Catalog() => new TestCatalog()
        .Add("CHA0001", 400000m, CatalogCategory.Chair, null)
        .Add("DSK0001", 800000m, CatalogCategory.Desk, null)
        .Add("MON0001", 300000m)
        .Add("PLT0001", 200000m, subCategory: CatalogSubCategory.Plant)
        .Add("CFE0001", 750000m, subCategory: CatalogSubCategory.Coffee)
        .Add("BBG0001", 350000m, subCategory: CatalogSubCategory.Beanbag);

    [Fact] // WS-05
    public async Task Assigning_by_sku_lands_in_the_right_slot_without_the_caller_choosing_one()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();
        var handler = new AssignProductHandler(context.Repository, Catalog());

        var view = await handler.HandleAsync(new AssignProduct(context.Token, "cfe0001"));

        workspace.AssignmentFor(SlotId.CoffeeStation)!.Sku.Should().Be("CFE0001");
        view.Slots.Single(slot => slot.Slot == SlotId.CoffeeStation).IsFilled.Should().BeTrue();
    }

    [Fact] // WS-13
    public async Task Assigning_a_sku_the_catalog_does_not_know_is_a_validation_error_not_a_crash()
    {
        var context = new WorkspaceTestContext();
        await context.DraftAsync();
        var handler = new AssignProductHandler(context.Repository, Catalog());

        var action = async () => await handler.HandleAsync(new AssignProduct(context.Token, "XXX0000"));

        await action.Should().ThrowAsync<DomainRuleViolationException>();
    }

    [Fact]
    public async Task A_command_against_an_unknown_draft_reports_not_found()
    {
        var context = new WorkspaceTestContext();
        var handler = new AssignProductHandler(context.Repository, Catalog());

        var action = async () => await handler.HandleAsync(new AssignProduct(context.Token, "CHA0001"));

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact] // WS-06
    public async Task Adding_a_monitor_from_the_product_grid_when_the_slot_is_full_is_refused()
    {
        var context = new WorkspaceTestContext();
        await context.DraftAsync();
        var handler = new AssignProductHandler(context.Repository, Catalog());

        await handler.HandleAsync(new AssignProduct(context.Token, "MON0001", 3));

        var action = async () => await handler.HandleAsync(new AssignProduct(context.Token, "MON0001"));

        await action.Should().ThrowAsync<DomainRuleViolationException>();
    }

    [Fact] // WS-08
    public async Task Removing_an_empty_slot_does_not_write_to_the_database()
    {
        var context = new WorkspaceTestContext();
        await context.DraftAsync();
        var handler = new RemoveAssignmentHandler(context.Repository, Catalog());

        await handler.HandleAsync(new RemoveAssignment(context.Token, SlotId.Plant));

        context.Repository.SaveCount.Should().Be(0);
    }

    [Fact] // WS-09
    public async Task Removing_through_the_handler_empties_the_slot_and_saves()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();
        await new AssignProductHandler(context.Repository, Catalog())
            .HandleAsync(new AssignProduct(context.Token, "PLT0001"));

        var view = await new RemoveAssignmentHandler(context.Repository, Catalog())
            .HandleAsync(new RemoveAssignment(context.Token, SlotId.Plant));

        workspace.AssignmentFor(SlotId.Plant).Should().BeNull();
        view.IsEmpty.Should().BeTrue();
        context.Repository.SaveCount.Should().Be(2);
    }

    [Fact] // WS-09
    public async Task Setting_a_quantity_of_zero_through_the_handler_empties_the_slot()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();
        await new AssignProductHandler(context.Repository, Catalog())
            .HandleAsync(new AssignProduct(context.Token, "MON0001", 2));

        await new ChangeQuantityHandler(context.Repository, Catalog())
            .HandleAsync(new ChangeQuantity(context.Token, SlotId.Monitor, 0));

        workspace.AssignmentFor(SlotId.Monitor).Should().BeNull();
    }

    [Fact] // WS-10
    public async Task Raising_a_quantity_to_the_capacity_works_through_the_handler()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();
        await new AssignProductHandler(context.Repository, Catalog())
            .HandleAsync(new AssignProduct(context.Token, "MON0001"));

        await new ChangeQuantityHandler(context.Repository, Catalog())
            .HandleAsync(new ChangeQuantity(context.Token, SlotId.Monitor, 3));

        workspace.AssignmentFor(SlotId.Monitor)!.Quantity.Should().Be(3);
    }

    [Fact] // ADDR-04
    public async Task Saving_the_address_writes_once_per_command_not_per_keystroke()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();
        var handler = new SetDeliveryAddressHandler(context.Repository, Catalog());

        await handler.HandleAsync(new SetDeliveryAddress(context.Token, "Villa Lotus, Canggu"));
        await handler.HandleAsync(new SetDeliveryAddress(context.Token, "Villa Lotus, Canggu, Bali"));

        context.Repository.SaveCount.Should().Be(2);
        workspace.DeliveryAddress.Should().Be("Villa Lotus, Canggu, Bali");
    }

    [Fact] // ADDR-01
    public async Task The_view_reports_whether_the_workspace_can_be_checked_out()
    {
        var context = new WorkspaceTestContext();
        await context.DraftAsync();
        var assign = new AssignProductHandler(context.Repository, Catalog());
        var read = new GetWorkspaceHandler(context.Repository, Catalog());

        (await read.HandleAsync(new GetWorkspace(context.Token))).CanCheckout.Should().BeFalse();

        await assign.HandleAsync(new AssignProduct(context.Token, "CHA0001"));

        var view = await read.HandleAsync(new GetWorkspace(context.Token));

        view.CanCheckout.Should().BeTrue();
        view.DeliveryAddress.Should().BeNull("an address is required by checkout but not to fill a workspace");
        view.Slots.Should().HaveCount(7);
    }

    [Fact] // WS-11
    public async Task The_quote_query_prices_the_draft()
    {
        var context = new WorkspaceTestContext();
        await context.DraftAsync();
        await new AssignProductHandler(context.Repository, Catalog())
            .HandleAsync(new AssignProduct(context.Token, "DSK0001"));

        var quote = await new GetWorkspaceQuoteHandler(context.Repository, Catalog())
            .HandleAsync(new GetWorkspaceQuote(context.Token));

        quote.MonthlySubtotal.Amount.Should().Be(800000m);
    }
}
