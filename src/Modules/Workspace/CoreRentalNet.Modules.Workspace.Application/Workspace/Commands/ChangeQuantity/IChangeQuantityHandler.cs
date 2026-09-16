namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.ChangeQuantity;

/// <summary>Sets how many of one product a slot holds; zero removes it.</summary>
/// <remarks>A command handler mutates state and returns nothing; the caller re-reads to render.</remarks>
public interface IChangeQuantityHandler
{
    Task HandleAsync(ChangeQuantityCommand command, CancellationToken cancellationToken = default);
}
