namespace CoreRentalNet.Agents.Shared.Guardrails.Arguments;

/// <summary>The size ceiling one tool's arguments may not exceed, drawn from the workflow's own bounds.</summary>
/// <remarks>
/// The numbers are the same ones the rephrasing stage is held to, so the backstop cannot disagree with the
/// stage's limit — the failure the README warns about when one bound is stated twice.
/// </remarks>
internal sealed record ToolArgumentLimits(
    int MaximumArguments,
    int MaximumStringLength,
    int MaximumCollectionItems);
