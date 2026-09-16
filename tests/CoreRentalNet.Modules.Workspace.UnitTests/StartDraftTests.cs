using AwesomeAssertions;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.AssignProduct;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.StartDraft;
using CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspace;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

public sealed class StartDraftTests
{
    private static WorkspaceTestContext NewContext()
    {
        var context = new WorkspaceTestContext();
        context.Catalog
            .Add("CHA0001", 400000m, CatalogCategory.Chair, null)
            .Add("DSK0001", 800000m, CatalogCategory.Desk, null);
        return context;
    }

    [Fact] // DR-07
    public async Task Starting_a_draft_creates_exactly_one_and_is_idempotent()
    {
        var context = NewContext();
        var handler = new StartDraftHandler(context.Repository, WorkspaceTestContext.Tokens);
        var read = new GetWorkspaceHandler(context.Repository, WorkspaceTestContext.Tokens, context.Views);

        await handler.HandleAsync(new StartDraftCommand(context.Token));
        var first = await read.HandleAsync(new GetWorkspaceQuery(context.Token));
        await handler.HandleAsync(new StartDraftCommand(context.Token));
        var second = await read.HandleAsync(new GetWorkspaceQuery(context.Token));

        first.WorkspaceId.Should().Be(second.WorkspaceId);
        first.IsEmpty.Should().BeTrue();
        first.Slots.Should().HaveCount(7);
        context.Repository.SaveCount.Should().Be(1, "the second call must not write another draft");
    }

    [Fact]
    public async Task Two_browsers_get_two_drafts()
    {
        var context = NewContext();
        var handler = new StartDraftHandler(context.Repository, WorkspaceTestContext.Tokens);
        var read = new GetWorkspaceHandler(context.Repository, WorkspaceTestContext.Tokens, context.Views);

        await handler.HandleAsync(new StartDraftCommand("browser-one"));
        var first = await read.HandleAsync(new GetWorkspaceQuery("browser-one"));
        await handler.HandleAsync(new StartDraftCommand("browser-two"));
        var second = await read.HandleAsync(new GetWorkspaceQuery("browser-two"));

        first.WorkspaceId.Should().NotBe(second.WorkspaceId);
    }

    [Fact]
    public async Task A_started_draft_can_be_filled_and_read_back()
    {
        var context = NewContext();
        var catalog = context.Catalog;
        await new StartDraftHandler(context.Repository, WorkspaceTestContext.Tokens).HandleAsync(new StartDraftCommand(context.Token));

        await new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, catalog, context.Workspaces)
            .HandleAsync(new AssignProductCommand(context.Token, "CHA0001"));
        await new AssignProductHandler(context.Repository, WorkspaceTestContext.Tokens, catalog, context.Workspaces)
            .HandleAsync(new AssignProductCommand(context.Token, "DSK0001"));

        var read = await new GetWorkspaceHandler(context.Repository, WorkspaceTestContext.Tokens, context.Views)
            .HandleAsync(new GetWorkspaceQuery(context.Token));

        read.TotalUnits.Should().Be(2);
        read.CanCheckout.Should().BeTrue();
    }
}
