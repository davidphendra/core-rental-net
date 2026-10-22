using System.Diagnostics;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Telemetry;

namespace CoreRentalNet.Agents.Shared.Guardrails.Pipeline;

/// <summary>The ordered chain every tool call passes through — the one place the order is stated.</summary>
/// <remarks>
/// <para>
/// The order is the order the guards were registered, so it lives in our composition root and not in MAF's
/// middleware composition, which is free to change between releases. A denied argument guard stops the chain
/// before the tool runs; the result guards run in order after it returns.
/// </para>
/// <para>
/// The two trust records are the pipeline's boundary markers: the raw result arrives as
/// <see cref="UntrustedToolResult"/> and leaves as <see cref="ValidatedToolResult"/>, so a reader can see where
/// the crossing is even though the guards themselves carry plain values.
/// </para>
/// </remarks>
internal sealed class ToolGuardPipeline(
    IReadOnlyList<IToolGuard> argumentGuards,
    IReadOnlyList<IToolResultGuard> resultGuards) : IToolGuardPipeline
{
    public async ValueTask<GuardrailDecision> BeforeAsync(ToolInvocation invocation, CancellationToken cancellationToken)
    {
        foreach (var guard in argumentGuards)
        {
            var runId = Activity.Current?.GetTagItem(WorkspaceTelemetry.RunId) as string;
            using var activity = WorkspaceTelemetry.ActivitySource.StartActivity("guardrail.before");
            activity?.SetTag(WorkspaceTelemetry.ToolName, invocation.ToolName);
            activity?.SetTag("guard", guard.GetType().Name);
            activity?.SetTag(WorkspaceTelemetry.RunId, runId);

            var decision = await guard.BeforeAsync(invocation, cancellationToken);
            activity?.SetTag("allowed", decision.Allowed);

            if (!decision.Allowed)
            {
                return decision;
            }
        }

        return GuardrailDecision.Allow();
    }

    public async ValueTask<ValidatedToolResult> AfterAsync(
        ToolInvocation invocation,
        UntrustedToolResult untrusted,
        CancellationToken cancellationToken)
    {
        var value = untrusted.Value;

        foreach (var guard in resultGuards)
        {
            var runId = Activity.Current?.GetTagItem(WorkspaceTelemetry.RunId) as string;
            using var activity = WorkspaceTelemetry.ActivitySource.StartActivity("guardrail.after");
            activity?.SetTag(WorkspaceTelemetry.ToolName, invocation.ToolName);
            activity?.SetTag("guard", guard.GetType().Name);
            activity?.SetTag(WorkspaceTelemetry.RunId, runId);

            value = await guard.InspectAsync(invocation, value, cancellationToken);
        }

        return new ValidatedToolResult(invocation.ToolName, value);
    }
}
