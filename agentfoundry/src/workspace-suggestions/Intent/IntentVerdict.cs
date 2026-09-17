namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>What the verifier decided about a request, and why when it refused.</summary>
/// <param name="IsWorkspaceRequest">Whether the request is about composing a workspace.</param>
/// <param name="Code">A <see cref="Vocabularies.ReasonCodes"/> value; only meaningful when refused.</param>
public sealed record IntentVerdict(bool IsWorkspaceRequest, string Code);
