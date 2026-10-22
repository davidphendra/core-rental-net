using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Whether a retry asks the catalogue a different question from the attempt before it.</summary>
/// <remarks>
/// A model can repeat its own search vocabulary after being told what the previous attempt found. Running that
/// answer again spends the whole attempt on the same catalogue result. This policy compares the words, not the
/// prose, and leaves the decision about what to do with the answer to the retry policy.
/// </remarks>
public sealed class WorkspaceRetryProgressPolicy
{
    /// <summary>True when at least one relevant component's search vocabulary changed.</summary>
    public bool WouldChangeSearch(
        WorkspaceRequirementExpansion? previous,
        WorkspaceRequirementExpansion? current,
        CatalogueProductRetrievalResult? previousSearches)
    {
        if (previous is null || current is null || previousSearches is null)
        {
            return true;
        }

        var emptyCategories = previousSearches.ComponentSearchOutcomes
            .Where(outcome => outcome.FoundProductCount == 0)
            .Select(outcome => outcome.ComponentCategory)
            .ToHashSet();

        if (emptyCategories.Count == 0)
        {
            return true;
        }

        foreach (var component in current.ComponentExpansions.Every()
            .Where(component => emptyCategories.Contains(component.ComponentCategory)))
        {
            if (!component.Expansion.IsRelevant)
            {
                return true;
            }

            var before = TermsFor(previous, component.ComponentCategory);
            var after = TermsFor(current, component.ComponentCategory);

            if (!before.SetEquals(after))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> TermsFor(
        WorkspaceRequirementExpansion expansion,
        WorkspaceComponentCategory componentCategory)
    {
        var component = expansion.ComponentExpansions.Every()
            .Single(candidate => candidate.ComponentCategory == componentCategory)
            .Expansion;

        return [.. component.SearchTerms.Concat(component.Synonyms)];
    }
}
