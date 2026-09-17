using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Specifications;

namespace AgentFoundry.WorkspaceSuggestions.Selection;

/// <summary>
/// Composes the candidates: reads the catalogue, fills each slot at each tier, and says what it could
/// not check.
/// </summary>
/// <remarks>
/// A port because it is the workflow's third node. It is also the only component in the run that touches
/// the catalogue, which is what keeps one credential, one read and one place to go wrong.
/// </remarks>
public interface ISelectCandidates
{
    /// <summary>
    /// How many times this instance has read the catalogue.
    /// </summary>
    /// <remarks>
    /// Exposed because the number is a cost decision rather than an implementation detail: a second
    /// read is an authenticated round trip for data already in hand, and a run should be able to say
    /// whether it made one.
    /// </remarks>
    int CatalogueReads { get; }

    /// <param name="specification">
    /// What the request means, including the words it was made of - so the criteria a candidate is
    /// checked against come from the same place the slots did.
    /// </param>
    Task<Selection> SelectAsync(
        Specification specification,
        CancellationToken cancellationToken = default);
}
