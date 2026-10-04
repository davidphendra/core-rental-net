namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>One tool call, in terms that do not mention MAF, so a policy is testable without it.</summary>
internal sealed record ToolInvocation(
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments);
