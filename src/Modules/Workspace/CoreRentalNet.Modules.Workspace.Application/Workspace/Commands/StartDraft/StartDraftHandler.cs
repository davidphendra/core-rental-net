using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Application.Queries.Services;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Services;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Support;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.StartDraft;

/// <summary>Starts a draft for a customer who has none; leaves an existing one untouched.</summary>
public sealed class StartDraftHandler(
    IWorkspaceRepository repository,
    IOpaqueTokenService tokens) : IStartDraftHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(StartDraftCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var token = new DraftToken(tokens.HashOf(command.DraftToken));
        var workspace = await repository.FindByTokenAsync(token, cancellationToken).ConfigureAwait(false);

        if (workspace is null)
        {
            workspace = new Domain.Workspace
            {
                Id = WorkspaceId.New(),
                DraftTokenHash = token.Hash,
                State = DraftState.Draft,
                Version = 1,
                Assignments = [],
            };

            await repository.AddAsync(workspace, cancellationToken).ConfigureAwait(false);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
