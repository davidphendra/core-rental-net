using System.Text.RegularExpressions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Keeps a model number the customer typed in the category that names it.</summary>
/// <remarks>
/// A rephraser can turn "Dell 3007WFP-HC 30-inch monitor" into "Dell 30-inch monitor" and lose the only token that
/// makes the product findable. The sentence is the source of truth, so a token that mixes letters and digits is
/// added to the nearest category's search terms when the rephraser dropped it.
/// </remarks>
public sealed class WorkspaceExplicitModelTermPolicy
{
    private static readonly Regex s_modelToken = new(
        @"\b(?=[A-Za-z0-9-]*[A-Za-z])(?=[A-Za-z0-9-]*[0-9])[A-Za-z0-9-]{4,}\b",
        RegexOptions.CultureInvariant);

    public WorkspaceRequirementExpansion Apply(WorkspaceRequirementExpansion expansion)
    {
        ArgumentNullException.ThrowIfNull(expansion);

        var additions = s_modelToken.Matches(expansion.OriginalCustomerQuery)
            .Select(match => (
                Category: WorkspaceExplicitCategoryReader.CategoryNearestTo(
                    expansion.OriginalCustomerQuery, match.Index),
                Term: match.Value))
            .Where(candidate => candidate.Category is not null)
            .GroupBy(candidate => candidate.Category!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Select(candidate => candidate.Term).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

        if (additions.Count == 0)
        {
            return expansion;
        }

        return expansion with
        {
            ComponentExpansions = expansion.ComponentExpansions with
            {
                Desk = WithTerms(expansion.ComponentExpansions.Desk, additions, WorkspaceComponentCategory.Desk),
                Chair = WithTerms(expansion.ComponentExpansions.Chair, additions, WorkspaceComponentCategory.Chair),
                Monitor = WithTerms(expansion.ComponentExpansions.Monitor, additions, WorkspaceComponentCategory.Monitor),
                Lamp = WithTerms(expansion.ComponentExpansions.Lamp, additions, WorkspaceComponentCategory.Lamp),
                Plant = WithTerms(expansion.ComponentExpansions.Plant, additions, WorkspaceComponentCategory.Plant),
                BeanBag = WithTerms(expansion.ComponentExpansions.BeanBag, additions, WorkspaceComponentCategory.BeanBag),
                CoffeeMachine = WithTerms(
                    expansion.ComponentExpansions.CoffeeMachine,
                    additions,
                    WorkspaceComponentCategory.CoffeeMachine),
            },
        };
    }

    private static WorkspaceComponentExpansion WithTerms(
        WorkspaceComponentExpansion expansion,
        IReadOnlyDictionary<WorkspaceComponentCategory, string[]> additions,
        WorkspaceComponentCategory category)
        => additions.TryGetValue(category, out var terms)
            ? expansion with
            {
                SearchTerms = [.. expansion.SearchTerms
                    .Concat(terms.Where(term => !expansion.SearchTerms.Contains(
                        term, StringComparer.OrdinalIgnoreCase)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)],
            }
            : expansion;
}
