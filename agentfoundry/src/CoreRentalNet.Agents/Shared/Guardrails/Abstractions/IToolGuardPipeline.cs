namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>The ordered guardrail pipeline: what a call passes through before and after the tool.</summary>
/// <remarks>
/// A port so the MAF adapter depends on the pipeline's shape and not on the class that happens to hold the
/// guards, which is what lets the order be stated once and tested without MAF.
/// </remarks>
internal interface IToolGuardPipeline
{
    ValueTask<GuardrailDecision> BeforeAsync(ToolInvocation invocation, CancellationToken cancellationToken);

    ValueTask<ValidatedToolResult> AfterAsync(
        ToolInvocation invocation,
        UntrustedToolResult untrusted,
        CancellationToken cancellationToken);
}
