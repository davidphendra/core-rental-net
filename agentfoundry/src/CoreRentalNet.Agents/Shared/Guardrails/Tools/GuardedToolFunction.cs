using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Guardrails.Tools;

/// <summary>One tool, run through the guardrail pipeline on the way in and on the way back.</summary>
/// <remarks>
/// <para>
/// <b>It wraps the tool rather than the function loop.</b> The catalogue's tools reach the model's options from
/// inside the chat-client chain, downstream of where the agent's function middleware transforms tools — so a tool
/// guarded there is a tool guarded nowhere. Wrapping the tool itself means the pipeline runs wherever the tool
/// was attached, and the ledger is written by the one path that also guards what the model is offered.
/// </para>
/// <para>
/// Everything a model may call stays the tool's own: the name, the description and the schema are delegated, so
/// wrapping a tool cannot change what a model may call.
/// </para>
/// </remarks>
internal sealed class GuardedToolFunction(
    AIFunction toolFunction,
    IToolGuardPipeline pipeline) : DelegatingAIFunction(toolFunction)
{
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var invocation = new ToolInvocation(Name, arguments);

        var decision = await pipeline.BeforeAsync(invocation, cancellationToken);

        if (!decision.Allowed)
        {
            // Returned rather than thrown: the function loop hands the reason back to the model, which may
            // correct the call. A throw would fail the stage and give the model no chance to.
            return decision.Reason ?? "The call was refused.";
        }

        var result = await base.InvokeCoreAsync(arguments, cancellationToken);

        var validated = await pipeline.AfterAsync(
            invocation,
            new UntrustedToolResult(invocation.ToolName, result),
            cancellationToken);

        return validated.Value;
    }
}
