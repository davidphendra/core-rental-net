using AgentFoundry.WorkspaceSuggestions.Contracts;

namespace AgentFoundry.WorkspaceSuggestions.Workflows;

/// <summary>What the verifier sends on: the request, and what it decided about it.</summary>
/// <remarks>
/// Both halves travel together because the graph branches on the decision and the nodes after it need
/// the request. Carrying the decision as a property rather than as a message type keeps one shape on
/// every edge, which is what makes the edge condition readable.
/// </remarks>
public sealed record Verification(SuggestionRequest Request, Intent.IntentVerdict Verdict);
