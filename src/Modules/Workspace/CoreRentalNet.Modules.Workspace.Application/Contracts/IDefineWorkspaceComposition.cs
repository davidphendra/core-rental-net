namespace CoreRentalNet.Modules.Workspace.Application.Contracts;

/// <summary>Workspace's public contract, used by Rentals when turning a draft into an order.</summary>
public interface IDefineWorkspaceComposition
{
    /// <summary>Returns the draft behind a raw cookie token, or null when there is no such draft.</summary>
    Task<WorkspaceComposition?> DefineCompositionAsync(string rawDraftToken, CancellationToken cancellationToken = default);
}
