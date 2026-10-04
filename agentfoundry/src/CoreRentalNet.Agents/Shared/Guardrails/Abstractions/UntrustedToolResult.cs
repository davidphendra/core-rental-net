namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>A tool's result as it arrives — unvalidated, and never handed to a model in this shape.</summary>
internal sealed record UntrustedToolResult(string ToolName, object? Value);
