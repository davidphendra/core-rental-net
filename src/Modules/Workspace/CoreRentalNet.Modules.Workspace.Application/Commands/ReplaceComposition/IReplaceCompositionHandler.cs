namespace CoreRentalNet.Modules.Workspace.Application.Commands.ReplaceComposition;

/// <summary>Replaces the composition behind a draft token in one write.</summary>
/// <remarks>
/// A command handler mutates state and returns nothing; the caller re-reads to render. It refuses rather than
/// returns a verdict: a workspace that changed underneath the caller, a composition that does not fit its
/// slots, and a draft that has already become an order are all exceptional, and all of them apply nothing.
/// </remarks>
public interface IReplaceCompositionHandler
{
    Task HandleAsync(ReplaceCompositionCommand command, CancellationToken cancellationToken = default);
}
