using CoreRentalNet.Modules.Workspace.Application.Queries.Views;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.ChangeQuantity;

/// <summary>Sets how many of one product a slot holds; zero removes it.</summary>
public interface IChangeQuantity
{
    Task<WorkspaceView> HandleAsync(ChangeQuantity command, CancellationToken cancellationToken = default);
}
