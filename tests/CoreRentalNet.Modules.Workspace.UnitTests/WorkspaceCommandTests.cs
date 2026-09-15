using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.AssignProduct;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.ChangeQuantity;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.RemoveAssignment;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.SetDeliveryAddress;
using CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspace;
using CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspaceQuote;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

public sealed class WorkspaceCommandTests
{
    private static WorkspaceTestContext NewContext()
    {
        var context = new WorkspaceTestContext();
        context.Catalog
            .Add("CHA0001", 400000m, CatalogCategory.Chair, null)
            .Add("DSK0001", 800000m, CatalogCategory.Desk, null)
            .Add("MON0001", 300000m)
            .Add("PLT0001", 200000m, subCategory: CatalogSubCategory.Plant)
            .Add("CFE0001", 750000m, subCategory: CatalogSubCategory.Coffee)
            .Add("BBG0001", 350000m, subCategory: CatalogSubCategory.Beanbag);
        return context;
    }

    [Fact] // WS-05
    public async Task Assigning_by_sku_lands_in_the_right_slot_without_the_caller_choosing_one()
    {
        var context = NewContext();
        var workspace = await context.DraftAsync();
        var handler = new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, context.Catalog, context.Workspaces, context.Views);

        var view = await handler.HandleAsync(new AssignProduct(context.Token, "cfe0001"));

        context.Queries.AssignmentsFor(workspace, SlotId.CoffeeStation).Single().Sku.Should().Be("CFE0001");
        view.Slots.Single(slot => slot.Slot == SlotId.CoffeeStation).IsFilled.Should().BeTrue();
    }

    [Fact] // WS-13
    public async Task Assigning_a_sku_the_catalog_does_not_know_is_a_validation_error_not_a_crash()
    {
        var context = NewContext();
        await context.DraftAsync();
        var handler = new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, context.Catalog, context.Workspaces, context.Views);

        var action = async () => await handler.HandleAsync(new AssignProduct(context.Token, "XXX0000"));

        await action.Should().ThrowAsync<DomainRuleViolationException>();
    }

    [Fact]
    public async Task A_command_against_an_unknown_draft_reports_not_found()
    {
        var context = NewContext();
        var handler = new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, context.Catalog, context.Workspaces, context.Views);

        var action = async () => await handler.HandleAsync(new AssignProduct(context.Token, "CHA0001"));

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact] // WS-06
    public async Task Adding_a_monitor_from_the_product_grid_when_the_slot_is_full_is_refused()
    {
        var context = NewContext();
        await context.DraftAsync();
        var handler = new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, context.Catalog, context.Workspaces, context.Views);

        await handler.HandleAsync(new AssignProduct(context.Token, "MON0001", 3));

        var action = async () => await handler.HandleAsync(new AssignProduct(context.Token, "MON0001"));

        await action.Should().ThrowAsync<DomainRuleViolationException>();
    }

    [Fact] // WS-08
    public async Task Removing_an_empty_slot_does_not_write_to_the_database()
    {
        var context = NewContext();
        await context.DraftAsync();
        var handler = new RemoveAssignmentHandler(context.Repository, WorkspaceTestContext.Tokens, context.Workspaces, context.Views);

        await handler.HandleAsync(new RemoveAssignment(context.Token, SlotId.Plant));

        context.Repository.SaveCount.Should().Be(0);
    }

    [Fact] // WS-09
    public async Task Removing_through_the_handler_empties_the_slot_and_saves()
    {
        var context = NewContext();
        var workspace = await context.DraftAsync();
        await new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, context.Catalog, context.Workspaces, context.Views)
            .HandleAsync(new AssignProduct(context.Token, "PLT0001"));

        var view = await new RemoveAssignmentHandler(context.Repository, WorkspaceTestContext.Tokens, context.Workspaces, context.Views)
            .HandleAsync(new RemoveAssignment(context.Token, SlotId.Plant));

        context.Queries.AssignmentsFor(workspace, SlotId.Plant).Should().BeEmpty();
        view.IsEmpty.Should().BeTrue();
        context.Repository.SaveCount.Should().Be(2);
    }

    [Fact] // WS-09
    public async Task Setting_a_quantity_of_zero_through_the_handler_empties_the_slot()
    {
        var context = NewContext();
        var workspace = await context.DraftAsync();
        await new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, context.Catalog, context.Workspaces, context.Views)
            .HandleAsync(new AssignProduct(context.Token, "MON0001", 2));

        await new ChangeQuantityHandler(context.Repository, WorkspaceTestContext.Tokens, context.Workspaces, context.Views)
            .HandleAsync(new ChangeQuantity(context.Token, SlotId.Monitor, "MON0001", 0));

        context.Queries.AssignmentsFor(workspace, SlotId.Monitor).Should().BeEmpty();
    }

    [Fact] // WS-10
    public async Task Raising_a_quantity_to_the_capacity_works_through_the_handler()
    {
        var context = NewContext();
        var workspace = await context.DraftAsync();
        await new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, context.Catalog, context.Workspaces, context.Views)
            .HandleAsync(new AssignProduct(context.Token, "MON0001"));

        await new ChangeQuantityHandler(context.Repository, WorkspaceTestContext.Tokens, context.Workspaces, context.Views)
            .HandleAsync(new ChangeQuantity(context.Token, SlotId.Monitor, "MON0001", 3));

        context.Queries.AssignmentsFor(workspace, SlotId.Monitor).Single().Quantity.Should().Be(3);
    }

    [Fact] // ADDR-04
    public async Task Saving_the_address_writes_once_per_command_not_per_keystroke()
    {
        var context = NewContext();
        var workspace = await context.DraftAsync();
        var handler = new SetDeliveryAddressHandler(context.Repository, WorkspaceTestContext.Tokens, context.Workspaces, context.Views);

        await handler.HandleAsync(new SetDeliveryAddress(context.Token, "Villa Lotus, Canggu"));
        await handler.HandleAsync(new SetDeliveryAddress(context.Token, "Villa Lotus, Canggu, Bali"));

        context.Repository.SaveCount.Should().Be(2);
        workspace.DeliveryAddress.Should().Be("Villa Lotus, Canggu, Bali");
    }

    [Fact] // ADDR-01
    public async Task The_view_reports_whether_the_workspace_can_be_checked_out()
    {
        var context = NewContext();
        await context.DraftAsync();
        var assign = new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, context.Catalog, context.Workspaces, context.Views);
        var read = new GetWorkspaceHandler(context.Repository, WorkspaceTestContext.Tokens, context.Views);

        (await read.HandleAsync(new GetWorkspace(context.Token))).CanCheckout.Should().BeFalse();

        await assign.HandleAsync(new AssignProduct(context.Token, "CHA0001"));

        var chaired = await read.HandleAsync(new GetWorkspace(context.Token));

        // A chair on its own is not a workspace: the desk is missing, and the view says which slot
        // it is rather than leaving the customer to guess why the way out is shut.
        chaired.CanCheckout.Should().BeFalse();
        chaired.BlockingReason.Should().Contain("desk");

        await assign.HandleAsync(new AssignProduct(context.Token, "DSK0001"));

        var view = await read.HandleAsync(new GetWorkspace(context.Token));

        view.CanCheckout.Should().BeTrue();
        view.BlockingReason.Should().BeNull();
        view.DeliveryAddress.Should().BeNull("an address is required by checkout but not to fill a workspace");
        view.Slots.Should().HaveCount(7);
    }

    [Fact] // WS-11
    public async Task The_quote_query_prices_the_draft()
    {
        var context = NewContext();
        await context.DraftAsync();
        await new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, context.Catalog, context.Workspaces, context.Views)
            .HandleAsync(new AssignProduct(context.Token, "DSK0001"));

        var quote = await new GetWorkspaceQuoteHandler(context.Repository, WorkspaceTestContext.Tokens, context.Quotes)
            .HandleAsync(new GetWorkspaceQuote(context.Token));

        quote.MonthlySubtotal.Amount.Should().Be(800000m);
    }
}
