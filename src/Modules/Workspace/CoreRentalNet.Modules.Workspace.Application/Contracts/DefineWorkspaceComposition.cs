using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Contracts;

public sealed class DefineWorkspaceComposition(IWorkspaceRepository repository) : IDefineWorkspaceComposition
{
    public async Task<WorkspaceComposition?> DefineCompositionAsync(
        string rawDraftToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawDraftToken))
        {
            return null;
        }

        var token = DraftToken.FromRawToken(rawDraftToken);
        var workspace = await repository.FindByTokenAsync(token, cancellationToken).ConfigureAwait(false);

        if (workspace is null)
        {
            return null;
        }

        return new WorkspaceComposition(
            workspace.Id.Value,
            workspace.Assignments
                .Select(assignment => new WorkspaceCompositionLine(assignment.Sku, assignment.Quantity))
                .ToArray(),
            workspace.DeliveryAddress);
    }
}
