using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Contracts;

namespace WorkspaceSuggestions.Agents;

/// <summary>The agents this deployable serves, as data.</summary>
/// <remarks>
/// The one place an agent's <b>name</b> and its prompt file are written down together. The name is also the
/// executor identity the workflow records, so renaming one here is a resume-compatibility decision rather
/// than a rename.
/// </remarks>
internal static class AgentRoster
{
    public static AgentProfile Rephraser { get; } = new(
        Name: "rephraser",
        PromptFileName: "rephraser.v1.md",
        Description: "Turns one sentence into a workspace specification: what the customer wants, before any product is chosen.",
        Output: ChatResponseFormat.ForJsonSchema<WorkspaceSpec>());

    public static AgentProfile Suggestor { get; } = new(
        Name: "suggestor",
        PromptFileName: "suggestor.v2.md",
        Description: "Composes candidate workspace setups from the catalogue and the specification it is handed.",
        Output: ChatResponseFormat.ForJsonSchema<SuggestionResult>());

    /// <summary>In the order the workflow runs them: the specification first, then the composition.</summary>
    public static IReadOnlyList<AgentProfile> All { get; } = [Rephraser, Suggestor];
}
