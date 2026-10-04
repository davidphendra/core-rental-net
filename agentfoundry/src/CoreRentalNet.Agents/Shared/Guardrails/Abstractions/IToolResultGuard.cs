namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>Sees a tool's result before the model does, and returns what the model may see instead.</summary>
/// <remarks>
/// The result is untrusted on entry. A guard returns the value unchanged when it has nothing to remove, which is
/// what lets the same pipeline carry an observer (the recorder) beside the guards.
/// </remarks>
internal interface IToolResultGuard
{
    ValueTask<object?> InspectAsync(ToolInvocation invocation, object? result, CancellationToken cancellationToken);
}
