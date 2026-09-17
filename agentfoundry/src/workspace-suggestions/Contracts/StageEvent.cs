namespace AgentFoundry.WorkspaceSuggestions.Contracts;

/// <summary>Where a run has got to, as an id the application turns into its own words.</summary>
/// <remarks>
/// An id rather than a sentence: the application owns every word a customer reads, so a stage renamed
/// here cannot change what appears on a page, and an id the application does not know renders nothing
/// rather than leaking.
/// </remarks>
public sealed record StageEvent : SuggestionMessage
{
    public override string Kind => "stage";

    /// <summary>One of <see cref="Vocabularies.Stages"/>.</summary>
    public required string Stage { get; init; }

    /// <summary>Which attempt this stage belongs to. The run is bounded at three.</summary>
    public required int Attempt { get; init; }
}
