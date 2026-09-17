namespace AgentFoundry.WorkspaceSuggestions.Vocabularies;

/// <summary>
/// Why a run refused, and the statuses a result may carry.
/// </summary>
/// <remarks>
/// Codes rather than sentences, for the same reason the stages are ids: the application maps them to
/// copy, so the words a customer reads are reviewed as copy and no model-written sentence reaches a
/// page. These strings are part of the contract - a caller branches on them - so adding one is a
/// change to the published contract rather than a detail of a response.
/// </remarks>
public static class ReasonCodes
{
    /// <summary>The verifier judged the request to be about something other than a workspace.</summary>
    public const string NotWorkspaceRequest = "not_workspace_request";

    /// <summary>The reviewer approved the candidates.</summary>
    public const string Ok = "ok";

    /// <summary>Three attempts were spent and the reviewer was still not satisfied.</summary>
    public const string Exhausted = "exhausted";

    /// <summary>The verifier refused the request.</summary>
    public const string Rejected = "rejected";
}
