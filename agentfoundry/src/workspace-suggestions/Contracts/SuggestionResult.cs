namespace AgentFoundry.WorkspaceSuggestions.Contracts;

/// <summary>What a run answers: one of the three statuses, and what travels with it.</summary>
/// <remarks>
/// All three are answers. A refusal is the verifier succeeding at its job rather than a failure, and an
/// exhausted run still carries what it composed - with the findings against it, so the caller can say
/// what could not be confirmed instead of pretending everything was.
/// </remarks>
public sealed record SuggestionResult : SuggestionMessage
{
    public override string Kind => "result";

    /// <summary>One of <see cref="Vocabularies.Statuses"/>.</summary>
    public required string Status { get; init; }

    /// <summary>How many attempts the run used. The run is bounded at three.</summary>
    public required int Attempts { get; init; }

    /// <summary>Why the run refused, when it refused. The application maps it to its own words.</summary>
    public string? Code { get; init; }

    /// <summary>The candidates, empty when the run refused.</summary>
    public IReadOnlyList<SuggestionOption> Options { get; init; } = [];

    /// <summary>What the reviewer still objected to, when the attempts ran out.</summary>
    public IReadOnlyList<Finding> Findings { get; init; } = [];
}
