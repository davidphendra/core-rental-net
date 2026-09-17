using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Application.Services;
using CoreRentalNet.Modules.Workspace.Application.Support;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands.ApplyComposition;

/// <summary>
/// Replaces a workspace's slots, in one write and only if nothing else has moved it.
/// </summary>
/// <remarks>
/// One operation rather than a removal followed by an assignment per line, and that is the point of it:
/// composed from the existing commands this would bump the version once per line, so another tab would
/// watch a workspace pass through states nobody asked for, and a failure in the middle would leave
/// something that is neither the old workspace nor the composition the customer accepted.
/// </remarks>
public sealed class ApplyCompositionHandler(
    IWorkspaceRepository repository,
    IOpaqueTokenService tokens,
    IWorkspaceService workspaceService) : IApplyCompositionHandler
{
    public async Task<CompositionApplyOutcome> HandleAsync(
        ApplyCompositionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workspace = await WorkspaceResolver
            .ResolveAsync(repository, tokens, command.DraftToken, cancellationToken)
            .ConfigureAwait(false);

        // Checked before anything is written, so a stale request costs a comparison rather than an undo.
        if (workspace.Version != command.ExpectedVersion)
        {
            return CompositionApplyOutcome.Stale;
        }

        workspaceService.Replace(workspace, command.Lines);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CompositionApplyOutcome.Applied;
    }
}
