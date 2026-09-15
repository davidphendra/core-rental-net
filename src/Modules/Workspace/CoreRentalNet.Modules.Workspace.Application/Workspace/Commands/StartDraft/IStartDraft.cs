using CoreRentalNet.Modules.Workspace.Application.Queries.Views;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.StartDraft;

/// <summary>Starts the workspace behind a draft token, or returns the one that already exists.</summary>
public interface IStartDraft
{
    Task<WorkspaceView> HandleAsync(StartDraft command, CancellationToken cancellationToken = default);
}
