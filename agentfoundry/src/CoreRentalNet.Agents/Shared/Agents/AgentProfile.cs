using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Agents;

/// <summary>One stage agent's identity, its prompt, its contract, and whether it may search the catalogue.</summary>
/// <remarks>
/// Data, not behaviour. A stage is a roster entry rather than a subclass, so adding a stage is a new profile and
/// nothing else. <see cref="UsesCatalogueTools"/> is per stage because the catalogue entitlement is per stage:
/// only the retrieval agent searches, and a tool offered to an agent that must compose from what it was handed
/// is an invitation to compose from memory instead.
/// </remarks>
public sealed record AgentProfile(
    string Name,
    string PromptFileName,
    string Description,
    ChatResponseFormat Output,
    bool UsesCatalogueTools = false);
