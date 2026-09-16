using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.AssignProduct;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.SetDeliveryAddress;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Composition;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

public sealed class WorkspaceCompositionTests
{
    [Fact]
    public async Task The_composition_is_a_frozen_snapshot_for_another_module()
    {
        var context = new WorkspaceTestContext();
        await context.DraftAsync();
        var catalog = new TestCatalog()
            .Add("CHA0001", 400000m, CatalogCategory.Chair, null)
            .Add("MON0001", 300000m);
        var assign = new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, catalog, context.Workspaces);
        await assign.HandleAsync(new AssignProductCommand(context.Token, "CHA0001"));
        await assign.HandleAsync(new AssignProductCommand(context.Token, "MON0001", 2));
        await new SetDeliveryAddressHandler(context.Repository, WorkspaceTestContext.Tokens, context.Workspaces)
            .HandleAsync(new SetDeliveryAddressCommand(context.Token, "Villa Lotus, Canggu"));

        IDefineWorkspaceComposition contract = new DefineWorkspaceComposition(context.Repository, WorkspaceTestContext.Tokens);

        var composition = await contract.DefineCompositionAsync(context.Token);

        composition.Should().NotBeNull();
        composition!.Lines.Should().HaveCount(2);
        composition.Lines.Should().Contain(line => line.Sku == "MON0001" && line.Quantity == 2);
        composition.TotalUnits.Should().Be(3);
        composition.DeliveryAddress.Should().Be("Villa Lotus, Canggu");
    }

    [Fact]
    public async Task An_unknown_token_yields_no_composition_rather_than_an_exception()
    {
        IDefineWorkspaceComposition contract = new DefineWorkspaceComposition(new InMemoryWorkspaceRepository(), WorkspaceTestContext.Tokens);

        (await contract.DefineCompositionAsync("never-issued")).Should().BeNull();
        (await contract.DefineCompositionAsync("  ")).Should().BeNull();
    }

    [Fact]
    public void The_composition_carries_no_prices()
    {
        var properties = typeof(WorkspaceCompositionLine).GetProperties().Select(property => property.Name).ToArray();

        properties.Should().Equal("Sku", "Quantity");
    }
}
