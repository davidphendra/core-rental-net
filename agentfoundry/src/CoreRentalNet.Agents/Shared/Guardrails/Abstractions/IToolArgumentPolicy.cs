namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>The rules one tool's arguments must satisfy — the strategy a tool name selects.</summary>
/// <remarks>
/// One policy per tool, so a second tool is a second class and never an edit to a shared guard. Which tools a
/// policy covers is the policy's own answer, not a switch in a resolver.
/// </remarks>
internal interface IToolArgumentPolicy
{
    bool AppliesTo(string toolName);

    GuardrailDecision Evaluate(ToolInvocation invocation);
}
