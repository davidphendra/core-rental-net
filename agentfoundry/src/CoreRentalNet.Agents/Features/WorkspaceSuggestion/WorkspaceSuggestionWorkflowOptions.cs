namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion;

/// <summary>The workflow's own bounds, stated rather than buried in code.</summary>
/// <remarks>
/// <para>
/// Four numbers decide how a run behaves: how many attempts it may make, how many retrieved products the composer
/// is shown, and how many words one component's search may carry and how long each may be. They are configuration,
/// so a deployment can change them without a rebuild, and each has a default that is correct for this catalogue.
/// </para>
/// <para>
/// <b><see cref="MaximumSearchTermCountPerComponent"/> and
/// <see cref="MaximumSearchTermCharacterCount"/> are also stated at the MCP tool boundary, and the two cannot be
/// one number:</b> the tool belongs to the application's solution and this to the agent's. Each side pins its own
/// with a test, and this is the side that makes the tool's refusal unreachable from this pipeline.
/// </para>
/// </remarks>
public sealed record WorkspaceSuggestionWorkflowOptions
{
    public int MaximumAttemptCount { get; init; } = 3;

    /// <summary>The most products each component may offer the reranker.</summary>
    /// <remarks>
    /// <b>Fifteen, and measured rather than chosen.</b> Run over the real catalogue with the terms a requirement
    /// expansion produces, the product a need describes lands within the top fifteen for every need the search can
    /// find at all. A single broad category term needs thirty, because its order is not a relevance order — the
    /// terms are what make fifteen enough, and <c>NameSearchPoolCharacterisationTests</c> holds those numbers.
    /// </remarks>
    public int MaximumRetrievedProductsPerComponentForReranking { get; init; } = 15;

    /// <summary>The most products each component may offer the composer.</summary>
    public int MaximumSelectedProductsPerComponent { get; init; } = 3;

    /// <summary>The most words one component's search may carry when a deployment states nothing else.</summary>
    public const int DefaultMaximumSearchTermCountPerComponent = 8;

    /// <summary>The longest one search term may be when a deployment states nothing else.</summary>
    public const int DefaultMaximumSearchTermCharacterCount = 80;

    /// <summary>The most words one component's search may carry, terms and synonyms together.</summary>
    public int MaximumSearchTermCountPerComponent { get; init; } = DefaultMaximumSearchTermCountPerComponent;

    /// <summary>The longest one search term may be.</summary>
    public int MaximumSearchTermCharacterCount { get; init; } = DefaultMaximumSearchTermCharacterCount;
}
