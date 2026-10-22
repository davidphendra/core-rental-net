using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Keeps a retriever from declaring the catalogue unavailable after it has searched it.</summary>
/// <remarks>
/// The retriever's contract has one unavailable answer: the tool failed, so nothing was searched. A model can
/// copy that answer from the prompt's example even when every search returned products. The searches are the
/// evidence; when there is at least one, the catalogue was available.
/// </remarks>
public sealed class CatalogueProductRetrievalNormalizer
{
    public CatalogueProductRetrievalResult Normalize(CatalogueProductRetrievalResult retrieval)
    {
        ArgumentNullException.ThrowIfNull(retrieval);

        return retrieval.ComponentSearchOutcomes.Count == 0
            ? retrieval
            : retrieval with { IsAvailable = true, UnavailableReason = null };
    }
}
