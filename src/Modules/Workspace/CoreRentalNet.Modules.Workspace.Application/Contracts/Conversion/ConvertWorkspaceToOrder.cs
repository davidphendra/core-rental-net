using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Application.Telemetry;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Composition;
using CoreRentalNet.Modules.Workspace.Application.Services;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Conversion;

/// <summary>The implementation behind <see cref="IConvertWorkspaceToOrder"/>.</summary>
public sealed class ConvertWorkspaceToOrder(
    IWorkspaceRepository repository,
    IOpaqueTokenService tokens,
    IWorkspaceQueryService queries,
    IWorkspaceService workspaces) : IConvertWorkspaceToOrder
{
    public async Task<WorkspaceConversion> DescribeAsync(
        string rawDraftToken,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveAsync(rawDraftToken, cancellationToken).ConfigureAwait(false);

        return Snapshot(workspace, wasAlreadyConverted: queries.IsConverted(workspace));
    }

    public async Task<WorkspaceConversion> ConvertAsync(
        string rawDraftToken,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveAsync(rawDraftToken, cancellationToken).ConfigureAwait(false);
        var alreadyConverted = queries.IsConverted(workspace);

        if (!alreadyConverted)
        {
            workspaces.MarkConverted(workspace);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Both endings are counted, so the tag has two values to compare: reaching this page again is a
        // different fact from the conversion that created the order, and told apart only by that tag.
        BusinessTelemetry.WorkspaceConversions.Add(
            1,
            new KeyValuePair<string, object?>("firstAttempt", !alreadyConverted));

        return Snapshot(workspace, alreadyConverted);
    }

    private static WorkspaceConversion Snapshot(Domain.Workspace workspace, bool wasAlreadyConverted)
        => new(
            workspace.Id.Value,
            workspace.Assignments
                .Select(assignment => new WorkspaceCompositionLine(assignment.Sku, assignment.Quantity))
                .ToArray(),
            workspace.DeliveryAddress,
            wasAlreadyConverted);

    private async Task<Domain.Workspace> ResolveAsync(string rawDraftToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawDraftToken))
        {
            throw new NotFoundException("This browser has no workspace yet.");
        }

        var token = new DraftToken(tokens.HashOf(rawDraftToken));

        return await repository.FindByTokenAsync(token, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("This browser has no workspace yet.");
    }
}
