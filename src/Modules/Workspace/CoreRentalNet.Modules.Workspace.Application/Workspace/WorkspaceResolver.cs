using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace;

internal static class WorkspaceResolver
{
    public static async Task<Domain.Workspace> ResolveAsync(
        IWorkspaceRepository repository,
        string rawDraftToken,
        CancellationToken cancellationToken)
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
