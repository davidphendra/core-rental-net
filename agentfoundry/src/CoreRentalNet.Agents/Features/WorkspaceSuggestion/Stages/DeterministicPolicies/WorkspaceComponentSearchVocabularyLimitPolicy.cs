namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

/// <summary>Bounds what one requirement expansion may ask a catalogue search to look for.</summary>
/// <remarks>
/// <para>
/// <b>It bounds rather than refuses, which is this repository's precedent rather than a concession.</b>
/// <see cref="WorkspaceComponentProductPoolPolicy"/> already drops the products past its bound without reporting
/// them, for the same reason: the workflow decides how much a search may carry, and a cosmetic overrun must not
/// cost a run. The hard refusal lives at the tool boundary, where there is a caller to tell.
/// </para>
/// <para>
/// <b>The count is stated here and again at the tool, and it cannot be stated once.</b> The tool belongs to the
/// application's solution and this to the agent's, so each side pins the number with its own test and the
/// contract is what keeps them equal.
/// </para>
/// <para>
/// <b>It runs before the retriever ever sees the expansion</b>, which is what makes the tool's refusal
/// unreachable from this application's own pipeline.
/// </para>
/// </remarks>
public sealed class WorkspaceComponentSearchVocabularyLimitPolicy(
    int maximumSearchTermCountPerComponent = 8,
    int maximumSearchTermCharacterCount = 80)
{
    private readonly int _maximumSearchTermCountPerComponent = maximumSearchTermCountPerComponent > 0
        ? maximumSearchTermCountPerComponent
        : throw new ArgumentOutOfRangeException(
            nameof(maximumSearchTermCountPerComponent),
            maximumSearchTermCountPerComponent,
            "The bound must be greater than zero.");

    private readonly int _maximumSearchTermCharacterCount = maximumSearchTermCharacterCount > 0
        ? maximumSearchTermCharacterCount
        : throw new ArgumentOutOfRangeException(
            nameof(maximumSearchTermCharacterCount),
            maximumSearchTermCharacterCount,
            "The bound must be greater than zero.");

    /// <summary>The same expansion, with every component's words bounded and nothing else changed.</summary>
    public WorkspaceRequirementExpansion ApplySearchVocabularyLimits(
        WorkspaceRequirementExpansion requirementExpansion)
    {
        ArgumentNullException.ThrowIfNull(requirementExpansion);

        return requirementExpansion with
        {
            ComponentExpansions = requirementExpansion.ComponentExpansions with
            {
                Desk = WithBoundedWords(requirementExpansion.ComponentExpansions.Desk),
                Chair = WithBoundedWords(requirementExpansion.ComponentExpansions.Chair),
                Monitor = WithBoundedWords(requirementExpansion.ComponentExpansions.Monitor),
                Lamp = WithBoundedWords(requirementExpansion.ComponentExpansions.Lamp),
                Plant = WithBoundedWords(requirementExpansion.ComponentExpansions.Plant),
                BeanBag = WithBoundedWords(requirementExpansion.ComponentExpansions.BeanBag),
                CoffeeMachine = WithBoundedWords(requirementExpansion.ComponentExpansions.CoffeeMachine),
            },
        };
    }

    /// <summary>One component's words, bounded: the primary terms first, then as many synonyms as fit.</summary>
    /// <remarks>
    /// <b>The order of the result is the point, not an accident of implementation.</b> A term is searched as an
    /// alternative and the answers are merged in the order the terms were given, so the primary terms come to be
    /// answered before the synonyms — the two-tier ranking that would otherwise have needed a mechanism of its
    /// own.
    /// </remarks>
    private WorkspaceComponentExpansion WithBoundedWords(WorkspaceComponentExpansion componentExpansion)
    {
        var searchTerms = componentExpansion.SearchTerms
            .Where(IsUsableSearchTerm)
            .Take(_maximumSearchTermCountPerComponent)
            .ToArray();

        var synonyms = componentExpansion.Synonyms
            .Where(IsUsableSearchTerm)
            .Take(_maximumSearchTermCountPerComponent - searchTerms.Length)
            .ToArray();

        return componentExpansion with { SearchTerms = searchTerms, Synonyms = synonyms };
    }

    /// <summary>Whether a term is worth searching with: present, and short enough for a tool to accept.</summary>
    private bool IsUsableSearchTerm(string searchTerm)
        => !string.IsNullOrWhiteSpace(searchTerm)
            && searchTerm.Length <= _maximumSearchTermCharacterCount;
}
