using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

namespace CoreRentalNet.Agents.Shared.Guardrails.Results;

/// <summary>Truncates a tool answer too large to hand a model, before it reaches one.</summary>
/// <remarks>
/// Sized for a catalogue page, not a data dump: an answer past this is either a broken tool or a hostile one, and
/// either way the model does not need all of it. The run continues on the truncated text rather than failing.
/// </remarks>
internal sealed class ToolResultSizeGuard(int maximumCharacters) : IToolResultGuard
{
    public ValueTask<object?> InspectAsync(ToolInvocation invocation, object? result, CancellationToken cancellationToken)
        => ValueTask.FromResult<object?>(
            result is string text && text.Length > maximumCharacters
                ? text[..maximumCharacters]
                : result);
}
