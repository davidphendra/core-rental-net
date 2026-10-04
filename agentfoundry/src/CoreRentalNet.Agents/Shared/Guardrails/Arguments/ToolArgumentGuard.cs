using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

namespace CoreRentalNet.Agents.Shared.Guardrails.Arguments;

/// <summary>Selects the one policy written for the tool being called, and runs it.</summary>
/// <remarks>
/// The tool name chooses the strategy, so a new tool is a new policy and never an edit here. A tool with no
/// policy is allowed: the allow-list has already decided it may run, and this guard only adds its own rules.
/// </remarks>
internal sealed class ToolArgumentGuard(IEnumerable<IToolArgumentPolicy> policies) : IToolGuard
{
    public ValueTask<GuardrailDecision> BeforeAsync(ToolInvocation invocation, CancellationToken cancellationToken)
    {
        var policy = policies.FirstOrDefault(candidate => candidate.AppliesTo(invocation.ToolName));

        return ValueTask.FromResult(policy?.Evaluate(invocation) ?? GuardrailDecision.Allow());
    }
}
