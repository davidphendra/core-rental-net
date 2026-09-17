using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Application.Commands.ApplyComposition;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.Modules.Workspace.UnitTests;

/// <summary>
/// Replacing a workspace's slots with a composition.
/// </summary>
/// <remarks>
/// The operation is destructive, so what these tests are really about is the things that make it safe to
/// offer: one write, one version, a stale request refused rather than allowed to win, and the delivery
/// address untouched - not because it is restored, but because it was never in the payload.
/// </remarks>
public sealed class ApplyCompositionTests
{
    private const int ChargingPorts = 1;

    [Fact] // AIB-21
    public async Task A_stale_version_applies_nothing_and_says_so()
    {
        var context = new WorkspaceTestContext();
        var draft = await context.DraftAsync();

        await context.AssignAsync(draft, SlotId.Desk, "DSK0001");

        // Somebody else changed the workspace after the composition was chosen - another tab, or
        // simply a long run.
        var elsewhere = draft.Version + ChargingPorts;

        var writes = context.Repository.SaveCount;

        var outcome = await ApplyAsync(context, draft, elsewhere, [Line("Chair", "CHA0001")]);

        outcome.Should().Be(CompositionApplyOutcome.Stale);

        // Nothing was written - not a smaller write, and not a corrected one - and the desk the customer
        // already had is still there.
        context.Repository.SaveCount.Should().Be(writes, "a stale request writes nothing at all");
        draft.Assignments.Should().ContainSingle().Which.Sku.Should().Be("DSK0001");
    }

    [Fact]
    public async Task The_composition_is_the_workspace_afterwards()
    {
        var context = new WorkspaceTestContext();
        var draft = await context.DraftAsync();

        await context.AssignAsync(draft, SlotId.Desk, "DSK0001");
        await context.AssignAsync(draft, SlotId.Chair, "CHA0001");

        var outcome = await ApplyAsync(
            context,
            draft,
            draft.Version,
            [Line("Desk", "DSK0002"), Line("Chair", "CHA0002"), Line("Monitor", "MON0001")]);

        outcome.Should().Be(CompositionApplyOutcome.Applied);

        // The composition is the contents, not an addition to them: the old assignment is gone.
        draft.Assignments.Should().HaveCount(3);
        draft.Assignments.Select(assignment => assignment.Sku)
            .Should().BeEquivalentTo(["DSK0002", "CHA0002", "MON0001"]);
    }

    /// <summary>
    /// It is one change, so it is one version.
    /// </summary>
    /// <remarks>
    /// This is the whole reason the operation exists rather than being composed from the commands that
    /// already do each part: a version per line would let another tab watch a workspace pass through
    /// states nobody asked for.
    /// </remarks>
    [Fact]
    public async Task Three_lines_bump_the_version_once()
    {
        var context = new WorkspaceTestContext();
        var draft = await context.DraftAsync();

        var before = draft.Version;

        await ApplyAsync(
            context,
            draft,
            before,
            [Line("Desk", "DSK0001"), Line("Chair", "CHA0001"), Line("Monitor", "MON0001")]);

        draft.Version.Should().Be(before + ChargingPorts);
    }

    [Fact]
    public async Task The_delivery_address_is_left_alone()
    {
        var context = new WorkspaceTestContext();
        var draft = await context.DraftAsync();

        draft.DeliveryAddress = "Villa Lotus, Canggu";

        await ApplyAsync(context, draft, draft.Version, [Line("Desk", "DSK0001")]);

        draft.DeliveryAddress.Should().Be("Villa Lotus, Canggu");
    }

    /// <summary>An unwritable line leaves the workspace as it was, not as half of the composition.</summary>
    [Fact]
    public async Task A_composition_with_a_line_that_cannot_be_written_changes_nothing()
    {
        var context = new WorkspaceTestContext();
        var draft = await context.DraftAsync();

        await context.AssignAsync(draft, SlotId.Desk, "DSK0001");

        var before = draft.Assignments.Select(assignment => assignment.Sku).ToList();
        var version = draft.Version;

        var apply = async () => await ApplyAsync(
            context,
            draft,
            version,
            [Line("Chair", "CHA0001"), Line("Nowhere", "MON0001")]);

        await apply.Should().ThrowAsync<DomainRuleViolationException>();

        draft.Assignments.Select(assignment => assignment.Sku).Should().BeEquivalentTo(before);
        draft.Version.Should().Be(version);
    }

    /// <summary>A composition that names a slot twice is not one answer, so it is refused.</summary>
    [Fact]
    public async Task A_slot_named_twice_is_refused()
    {
        var context = new WorkspaceTestContext();
        var draft = await context.DraftAsync();

        var apply = async () => await ApplyAsync(
            context,
            draft,
            draft.Version,
            [Line("Monitor", "MON0001"), Line("Monitor", "MON0002")]);

        await apply.Should().ThrowAsync<DomainRuleViolationException>();
        draft.Assignments.Should().BeEmpty();
    }

    [Fact]
    public async Task A_workspace_that_became_an_order_can_no_longer_be_replaced()
    {
        var context = new WorkspaceTestContext();
        var draft = await context.DraftAsync();

        draft.State = DraftState.Converted;

        var apply = async () => await ApplyAsync(context, draft, draft.Version, [Line("Desk", "DSK0001")]);

        await apply.Should().ThrowAsync<DomainRuleViolationException>();
    }

    private static CompositionLine Line(string slot, string sku) => new(slot, sku, Quantity: 1);

    private static async Task<CompositionApplyOutcome> ApplyAsync(
        WorkspaceTestContext context,
        Domain.Workspace draft,
        int expectedVersion,
        IReadOnlyList<CompositionLine> lines)
        => await new ApplyCompositionHandler(
                context.Repository,
                WorkspaceTestContext.Tokens,
                context.Workspaces)
            .HandleAsync(new ApplyCompositionCommand(context.Token, expectedVersion, lines));
}
