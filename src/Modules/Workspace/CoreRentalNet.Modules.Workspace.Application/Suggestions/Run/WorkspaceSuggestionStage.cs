namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;

/// <summary>The words the application puts around a run, and the only ones it says while one is going.</summary>
/// <remarks>
/// A run is a paid model call and a wait of the better part of a minute, so the customer is told what is
/// happening - in this application's words rather than in the agent's. None of these is a thing the agent can
/// name, which is why the agent is not asked for it.
/// </remarks>
public static class WorkspaceSuggestionStage
{
    /// <summary>The customer's sentence is being read and turned into a specification.</summary>
    public const string Reading = "Reading your request";

    /// <summary>What the customer asked for is being worked out.</summary>
    public const string Understanding = "Understanding your requirements";

    /// <summary>The catalogue the agent searches is being matched against that specification.</summary>
    public const string Matching = "Matching the catalogue";

    /// <summary>What the catalogue offered is being ordered against what the customer asked for.</summary>
    public const string Choosing = "Choosing the best matches";

    /// <summary>Candidate setups are being composed from what the catalogue offered.</summary>
    public const string Composing = "Composing the workspaces";

    /// <summary>What was composed is being checked against the catalogue.</summary>
    public const string Validating = "Checking the workspaces";

    /// <summary>What was composed is being reviewed against the request.</summary>
    public const string Reviewing = "Reviewing the workspaces";

    /// <summary>A stage the application does not recognise, said without naming the agent's vocabulary.</summary>
    public const string Working = "Working on your request";

    /// <summary>The application's words for the stage the agent named.</summary>
    /// <remarks>
    /// The agent's stage names are the workflow's, and the customer's words are this application's. Mapping one
    /// to the other here keeps the copy changeable without a workflow change, and keeps an unknown stage from
    /// reaching a customer as a word nobody wrote.
    /// </remarks>
    public static string WordsFor(string agentProcessingStage)
        => agentProcessingStage switch
        {
            "verifyingRequest" => Reading,
            "rephrasingRequirement" => Understanding,
            "retrievingCatalogueProducts" => Matching,
            "rerankingWorkspaceCandidates" => Choosing,
            "composingWorkspaceSetups" => Composing,
            "validatingWorkspaceSetups" => Validating,
            "reviewingWorkspaceSetups" => Reviewing,
            _ => Working,
        };
}
