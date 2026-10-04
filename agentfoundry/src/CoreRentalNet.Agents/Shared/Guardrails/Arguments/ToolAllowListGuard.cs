using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

namespace CoreRentalNet.Agents.Shared.Guardrails.Arguments;

/// <summary>Refuses any tool that is not on the list the deployment allows.</summary>
/// <remarks>
/// Generic: the allowed names arrive through <see cref="IToolAllowList"/>, so this guard does not know which
/// catalogue, if any, the run is searching.
/// </remarks>
internal sealed class ToolAllowListGuard(IToolAllowList allowList) : IToolGuard
{
    public ValueTask<GuardrailDecision> BeforeAsync(ToolInvocation invocation, CancellationToken cancellationToken)
        => ValueTask.FromResult(
            allowList.Contains(invocation.ToolName)
                ? GuardrailDecision.Allow()
                : GuardrailDecision.Deny(
                    $"The call was refused: no tool named '{invocation.ToolName}' is permitted."));
}
