using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>
/// The adapter's port. Its signature names MAF types on purpose: this is the one boundary that must know MAF and
/// everything on the other side of it must not, so the coupling is stated once rather than leaked into policies.
/// </summary>
internal interface IGuardrailFunctionMiddleware
{
    ValueTask<object?> InvokeAsync(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken);
}
