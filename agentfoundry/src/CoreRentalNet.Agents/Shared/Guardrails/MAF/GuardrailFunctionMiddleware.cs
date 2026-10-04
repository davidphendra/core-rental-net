using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

namespace CoreRentalNet.Agents.Shared.Guardrails.MAF;

/// <summary>Runs the guardrail pipeline around every tool call; the only type here coupled to MAF.</summary>
/// <remarks>
/// It does two things and no more: translate MAF's invocation into a <see cref="ToolInvocation"/>, and translate
/// the pipeline's outcome back. Nothing else in the guardrail folders references MAF, so every policy is
/// testable as ordinary code.
/// </remarks>
internal sealed class GuardrailFunctionMiddleware(IToolGuardPipeline pipeline) : IGuardrailFunctionMiddleware
{
    public async ValueTask<object?> InvokeAsync(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        var invocation = new ToolInvocation(context.Function.Name, context.Arguments);

        var decision = await pipeline.BeforeAsync(invocation, cancellationToken);

        if (!decision.Allowed)
        {
            // Returned rather than thrown: the function loop hands the reason back to the model, which may
            // correct the call. A throw would fail the stage and give the model no chance to.
            return decision.Reason ?? "The call was refused.";
        }

        var result = await next(context, cancellationToken);

        var validated = await pipeline.AfterAsync(
            invocation,
            new UntrustedToolResult(invocation.ToolName, result),
            cancellationToken);

        return validated.Value;
    }
}
