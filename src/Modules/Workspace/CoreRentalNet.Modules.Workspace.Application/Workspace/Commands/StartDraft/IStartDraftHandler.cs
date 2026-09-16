namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.StartDraft;

/// <summary>Starts the workspace behind a draft token, or leaves the one that already exists in place.</summary>
/// <remarks>
/// A command handler mutates state and returns nothing; the caller re-reads through
/// <c>IGetWorkspaceHandler</c> when it needs the canvas. The idempotency of the start is unchanged.
/// </remarks>
public interface IStartDraftHandler
{
    Task HandleAsync(StartDraftCommand command, CancellationToken cancellationToken = default);
}
