namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;

/// <summary>What the panel is given when a run ends: candidates, or the typed not-a-workspace verdict.</summary>
/// <remarks>
/// A refusal is a <b>result</b> rather than a failure, so it travels here with no candidates rather than as an
/// error - the application words it, and there is nothing for a customer to retry. The frame is the
/// application's, because one shape for the page is the application's to fix whatever the model said.
/// </remarks>
public sealed record WorkspaceSuggestionResultFrame(string Status, IReadOnlyList<WorkspaceSuggestionCandidate> Candidates)
{
    /// <summary>The agent proposed candidates, and they are shown as it stated them.</summary>
    public const string Suggested = "suggested";

    /// <summary>The typed verdict that the request was not about a workspace.</summary>
    public const string NotWorkspace = "notWorkspace";

    public static WorkspaceSuggestionResultFrame Refused()
        => new(NotWorkspace, []);

    public static WorkspaceSuggestionResultFrame Of(IReadOnlyList<WorkspaceSuggestionCandidate> candidates)
        => new(Suggested, candidates);
}
