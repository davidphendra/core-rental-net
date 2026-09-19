using Microsoft.Extensions.AI;

namespace WorkspaceSuggestions.Agents;

/// <summary>One agent's identity, its prompt, and the contract its answer must satisfy.</summary>
/// <remarks>
/// Data, not behaviour. A role is a roster entry rather than a subclass, so adding the second agent is a new
/// profile and nothing else — which is the property ADR 0003 claims for this layout, and the reason there is
/// no <c>RephraserAgent : SuggestorAgent</c> anywhere.
/// </remarks>
internal sealed record AgentProfile(
    string Name,
    string PromptFileName,
    string Description,
    ChatResponseFormat Output);
