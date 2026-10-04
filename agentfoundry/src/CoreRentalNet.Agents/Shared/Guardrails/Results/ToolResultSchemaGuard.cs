using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

namespace CoreRentalNet.Agents.Shared.Guardrails.Results;

/// <summary>Replaces a tool answer whose shape the tool's own contract does not recognise.</summary>
/// <remarks>
/// The shape is checked by a validator the feature supplies, so this guard never learns the catalogue's contract.
/// An unreadable answer becomes a short marker rather than an exception: the run is already at an ending, and the
/// pool builder still refuses to build from data it cannot read, which is the loud failure.
/// </remarks>
internal sealed class ToolResultSchemaGuard(IEnumerable<IToolResultValidator> validators) : IToolResultGuard
{
    private const string Unreadable = "The tool answered in a shape this agent could not read.";

    public ValueTask<object?> InspectAsync(ToolInvocation invocation, object? result, CancellationToken cancellationToken)
    {
        var validator = validators.FirstOrDefault(candidate => candidate.AppliesTo(invocation.ToolName));

        return ValueTask.FromResult<object?>(
            validator is null || validator.IsValid(result) ? result : Unreadable);
    }
}
