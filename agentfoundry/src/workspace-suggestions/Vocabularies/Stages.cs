namespace AgentFoundry.WorkspaceSuggestions.Vocabularies;

/// <summary>
/// The stages a run passes through, named once.
/// </summary>
/// <remarks>
/// Ids, not sentences. The application owns every word a customer reads, so the map from these to copy
/// lives there - and a stage renamed here changes a label rather than a page's contents. The schema
/// closes this list: a stage outside it is a contract failure, not a run that quietly says nothing.
/// </remarks>
public static class Stages
{
    /// <summary>Is this a request about a workspace at all?</summary>
    public const string Verifying = "verifying";

    /// <summary>Turning the request into a specification: slots, quantities and criteria.</summary>
    public const string Rephrasing = "rephrasing";

    /// <summary>Reading the catalogue and composing the candidates.</summary>
    public const string Selecting = "selecting";

    /// <summary>Checking the candidates, and objecting when they do not hold up.</summary>
    public const string Reviewing = "reviewing";

    /// <summary>Every stage, in the order a run passes through them.</summary>
    public static readonly IReadOnlyList<string> InOrder = [Verifying, Rephrasing, Selecting, Reviewing];
}
