using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.Modules.Workspace.Application.Services;
using CoreRentalNet.Modules.Workspace.Application.Support;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands.ReplaceComposition;

/// <summary>Replaces everything a draft holds with a composition, in one save.</summary>
/// <remarks>
/// The translation is all this does: it resolves the draft, turns the command's lines into the assignments the
/// module stores, hands them to the service that owns the rules, and saves <b>once</b> - which is what makes
/// the replacement one write rather than a delete and a series of adds.
/// </remarks>
public sealed class ReplaceCompositionHandler(
    IWorkspaceRepository repository,
    IOpaqueTokenService tokens,
    IWorkspaceCompositionService compositions) : IReplaceCompositionHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(ReplaceCompositionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var workspace = await WorkspaceResolver
            .ResolveAsync(repository, tokens, command.DraftToken, cancellationToken)
            .ConfigureAwait(false);

        var composition = command.Lines
            .Select(line => new SlotAssignment { Slot = line.Slot, Sku = line.Sku, Quantity = line.Quantity })
            .ToArray();

        compositions.ReplaceComposition(workspace, composition, command.ExpectedVersion);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
