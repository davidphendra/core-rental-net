namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>A tool's result once every result guard has passed — the only shape the model is given.</summary>
internal sealed record ValidatedToolResult(string ToolName, object? Value);
