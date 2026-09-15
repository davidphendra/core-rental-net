using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Support;

internal static class WorkspaceResolver
{
    public static async Task<Domain.Workspace> ResolveAsync(
        IWorkspaceRepository repository,
        IOpaqueTokenService tokens,
        string rawDraftToken,
        CancellationToken cancellationToken)
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
