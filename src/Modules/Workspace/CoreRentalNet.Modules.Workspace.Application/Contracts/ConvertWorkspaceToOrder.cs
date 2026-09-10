using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Contracts;

public sealed class ConvertWorkspaceToOrder(IWorkspaceRepository repository) : IConvertWorkspaceToOrder
{
    public async Task<WorkspaceConversion> DescribeAsync(
        string rawDraftToken,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveAsync(rawDraftToken, cancellationToken).ConfigureAwait(false);

        return Snapshot(workspace, wasAlreadyConverted: workspace.IsConverted);
    }

    public async Task<WorkspaceConversion> ConvertAsync(
        string rawDraftToken,
        CancellationToken cancellationToken = default)
    {
        var workspace = await ResolveAsync(rawDraftToken, cancellationToken).ConfigureAwait(false);
        var alreadyConverted = workspace.IsConverted;

        if (!alreadyConverted)
        {
            workspace.MarkConverted();
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

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

        var token = DraftToken.FromRawToken(rawDraftToken);

        return await repository.FindByTokenAsync(token, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("This browser has no workspace yet.");
    }
}
