using System.Collections;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

namespace CoreRentalNet.Agents.Shared.Guardrails.Arguments;

/// <summary>Refuses arguments too large to validate cheaply, before anything expensive reads them.</summary>
/// <remarks>
/// Model-supplied arguments are untrusted, and an argument can be megabytes of text or a list of a million
/// entries. Checking the size first is what keeps the checks that follow cheap.
/// </remarks>
internal sealed class ArgumentCeilingGuard(ToolArgumentLimits limits) : IToolGuard
{
    public ValueTask<GuardrailDecision> BeforeAsync(ToolInvocation invocation, CancellationToken cancellationToken)
    {
        if (invocation.Arguments.Count > limits.MaximumArguments)
        {
            return Refuse("more arguments than this tool accepts");
        }

        foreach (var argument in invocation.Arguments)
        {
            if (TooLarge(argument.Value))
            {
                return Refuse($"the argument '{argument.Key}' is larger than this tool accepts");
            }
        }

        return ValueTask.FromResult(GuardrailDecision.Allow());

        ValueTask<GuardrailDecision> Refuse(string reason)
            => ValueTask.FromResult(GuardrailDecision.Deny($"The call was refused: {reason}."));
    }

    private bool TooLarge(object? value) => value switch
    {
        string text => text.Length > limits.MaximumStringLength,
        ICollection items => items.Count > limits.MaximumCollectionItems,
        _ => false,
    };
}
