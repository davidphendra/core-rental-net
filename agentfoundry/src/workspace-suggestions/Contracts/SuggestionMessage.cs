namespace AgentFoundry.WorkspaceSuggestions.Contracts;

/// <summary>
/// One message of a run: a stage transition while it happens, or the result at the end of it.
/// </summary>
/// <remarks>
/// A union with a discriminator rather than two streams, because the caller must render them in the
/// order they occurred: a result that overtook the stage it followed would describe a run the customer
/// did not watch.
/// </remarks>
public abstract record SuggestionMessage
{
    /// <summary>The discriminator the contract carries, so a caller can branch without guessing.</summary>
    public abstract string Kind { get; }

    /// <summary>Ties every message to the run that produced it.</summary>
    public required string RequestId { get; init; }
}
