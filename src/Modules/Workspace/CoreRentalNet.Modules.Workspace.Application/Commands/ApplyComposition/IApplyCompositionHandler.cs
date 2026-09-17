namespace CoreRentalNet.Modules.Workspace.Application.Commands.ApplyComposition;

public interface IApplyCompositionHandler
{
    Task<CompositionApplyOutcome> HandleAsync(
        ApplyCompositionCommand command,
        CancellationToken cancellationToken = default);
}
