using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Commands.AssignProduct;
using CoreRentalNet.Modules.Workspace.Application.Commands.ReplaceComposition;
using CoreRentalNet.Modules.Workspace.Application.Commands.SetDeliveryAddress;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

/// <summary>
/// AIWB-36, AIWB-37 and AIWB-38: replacing a composition is one write, it leaves everything that is not the
/// composition alone, and a stale write applies nothing.
/// </summary>
/// <remarks>
/// The command exists because the AI suggestion, and any future "use this setup" path, replaces a workspace
/// whole. Doing that as a remove followed by a series of adds would be several writes, so a customer could
/// see a workspace that was half of each.
/// </remarks>
public sealed class ReplaceCompositionTests
{
    [Fact] // AIWB-36
    public async Task The_whole_composition_is_replaced_in_one_write()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();

        await AssignAsync(context, "DSK0001");
        await AssignAsync(context, "CHA0001");

        var savesBefore = context.Repository.SaveCount;

        await ReplaceAsync(context, workspace.Version, ("MON0001", 2, SlotId.Monitor));

        workspace.Assignments.Should().HaveCount(1);
        workspace.Assignments[0].Sku.Should().Be("MON0001");
        workspace.Assignments[0].Slot.Should().Be(SlotId.Monitor);
        workspace.Assignments[0].Quantity.Should().Be(2);

        // One save, not a delete and a series of adds: the repository is the only thing that writes, and it was
        // asked once. A caller can never observe a half-replaced workspace.
        context.Repository.SaveCount.Should().Be(
            savesBefore + 1,
            "the replacement is one write rather than one per line");
    }

    [Fact] // AIWB-37, and the contract this command exists to keep
    public async Task Replacing_a_composition_does_not_touch_the_delivery_address()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();

        await AssignAsync(context, "DSK0001");
        await new SetDeliveryAddressHandler(context.Repository, WorkspaceTestContext.Tokens, context.Workspaces)
            .HandleAsync(new SetDeliveryAddressCommand(context.Token, "Villa Lotus, Canggu"));

        await ReplaceAsync(context, workspace.Version, ("CHA0001", 1, SlotId.Chair));

        workspace.DeliveryAddress.Should().Be(
            "Villa Lotus, Canggu",
            "a customer who asked for a different setup did not ask to be re-addressed");
    }

    [Fact]
    public async Task Everything_that_is_not_the_composition_is_left_as_it_was()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();

        var id = workspace.Id;
        var token = workspace.DraftTokenHash;
        var state = workspace.State;

        await ReplaceAsync(context, workspace.Version, ("DSK0001", 1, SlotId.Desk));

        workspace.Id.Should().Be(id);
        workspace.DraftTokenHash.Should().Be(token);
        workspace.State.Should().Be(state, "replacing a setup is not converting it");
    }

    [Fact] // AIWB-38
    public async Task A_write_carrying_a_stale_version_is_refused_and_applies_nothing()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();

        await AssignAsync(context, "DSK0001");

        // What a customer read before somebody else's tab added a chair: the version they saw is now behind.
        var staleVersion = workspace.Version - 1;

        var act = async () => await ReplaceAsync(context, staleVersion, ("MON0001", 1, SlotId.Monitor));

        await act.Should().ThrowAsync<DomainRuleViolationException>();

        workspace.Assignments.Should().ContainSingle();
        workspace.Assignments[0].Sku.Should().Be("DSK0001", "a refused replacement leaves what it found");
    }

    [Fact]
    public async Task The_version_moves_when_a_composition_is_replaced()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();

        var before = workspace.Version;

        await ReplaceAsync(context, before, ("DSK0001", 1, SlotId.Desk));

        workspace.Version.Should().Be(before + 1, "a forgotten bump is a silent lost update");
    }

    [Fact]
    public async Task A_composition_that_does_not_fit_its_slots_is_refused_and_applies_nothing()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();

        await AssignAsync(context, "DSK0001");

        // The desk slot holds one, and this asks for two of them.
        var act = async () => await ReplaceAsync(context, workspace.Version, ("DSK0001", 2, SlotId.Desk));

        await act.Should().ThrowAsync<DomainRuleViolationException>();

        workspace.Assignments.Should().ContainSingle();
        workspace.Assignments[0].Sku.Should().Be("DSK0001");
    }

    [Fact]
    public async Task A_line_of_nothing_is_refused()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();

        var act = async () => await ReplaceAsync(context, workspace.Version, ("DSK0001", 0, SlotId.Desk));

        await act.Should().ThrowAsync<DomainRuleViolationException>();

        workspace.Assignments.Should().BeEmpty();
    }

    [Fact]
    public async Task A_workspace_that_has_become_an_order_cannot_be_replaced()
    {
        var context = new WorkspaceTestContext();
        var workspace = await context.DraftAsync();

        context.Workspaces.MarkConverted(workspace);

        var act = async () => await ReplaceAsync(context, workspace.Version, ("DSK0001", 1, SlotId.Desk));

        await act.Should().ThrowAsync<DomainRuleViolationException>();
    }

    [Fact]
    public async Task A_draft_that_does_not_exist_is_refused_rather_than_created()
    {
        var context = new WorkspaceTestContext();
        var handler = new ReplaceCompositionHandler(context.Repository, WorkspaceTestContext.Tokens, context.Compositions);

        var act = async () => await handler.HandleAsync(new ReplaceCompositionCommand(
            "a-token-nobody-issued",
            1,
            [new ReplaceCompositionLine(SlotId.Desk, "DSK0001", 1)]));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Puts a product in a slot the way the page does, and keeps the version honest.</summary>
    private static async Task AssignAsync(WorkspaceTestContext context, string sku)
    {
        var catalogue = new TestCatalogService()
            .Add("DSK0001", 400_000m, CatalogCategory.Desk, null)
            .Add("CHA0001", 400_000m, CatalogCategory.Chair, null)
            .Add("MON0001", 300_000m);

        await new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, catalogue, context.Workspaces)
            .HandleAsync(new AssignProductCommand(context.Token, sku));
    }

    private static Task ReplaceAsync(
        WorkspaceTestContext context,
        int expectedVersion,
        params (string Sku, int Quantity, SlotId Slot)[] lines)
        => new ReplaceCompositionHandler(context.Repository, WorkspaceTestContext.Tokens, context.Compositions)
            .HandleAsync(new ReplaceCompositionCommand(
                context.Token,
                expectedVersion,
                [.. lines.Select(line => new ReplaceCompositionLine(line.Slot, line.Sku, line.Quantity))]));
}
