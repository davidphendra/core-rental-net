namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>One check made before a tool runs. A guard the call does not concern returns Allow.</summary>
internal interface IToolGuard
{
    ValueTask<GuardrailDecision> BeforeAsync(ToolInvocation invocation, CancellationToken cancellationToken);
}
