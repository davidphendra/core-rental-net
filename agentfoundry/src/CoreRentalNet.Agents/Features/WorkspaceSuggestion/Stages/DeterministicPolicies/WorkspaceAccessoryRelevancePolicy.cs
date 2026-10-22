using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Keeps an unrequested accessory out of a workspace even when a model marked it relevant.</summary>
/// <remarks>
/// <para>
/// A room type is not a request for a monitor, a lamp or a plant. The rephraser is told that, but the answer is
/// still a model's. This policy checks the words the customer actually wrote and removes an accessory whose
/// catalogue noun does not occur there.
/// </para>
/// <para>
/// <b>Desk and chair are not touched.</b> Every workspace needs them, and a broad request may leave them implied.
/// The accessories are the optional additions whose presence changes the requested set.
/// </para>
/// </remarks>
public sealed class WorkspaceAccessoryRelevancePolicy
{
    /// <summary>The same expansion, with every unnamed accessory switched off.</summary>
    public WorkspaceRequirementExpansion ApplyRequestedAccessories(
        WorkspaceRequirementExpansion requirementExpansion)
    {
        ArgumentNullException.ThrowIfNull(requirementExpansion);

        var query = requirementExpansion.OriginalCustomerQuery;
        var components = requirementExpansion.ComponentExpansions;

        return requirementExpansion with
        {
            ComponentExpansions = new WorkspaceComponentExpansions(
                Desk: components.Desk,
                Chair: components.Chair,
                Monitor: ForAccessory(components.Monitor, query, "monitor", "screen", "display"),
                Lamp: ForAccessory(components.Lamp, query, "lamp", "lighting", "light"),
                Plant: ForAccessory(components.Plant, query, "plant", "succulent", "greenery", "tree"),
                BeanBag: ForAccessory(components.BeanBag, query, "bean bag", "beanbag", "bean-bag"),
                CoffeeMachine: ForAccessory(
                    components.CoffeeMachine,
                    query,
                    "coffee machine",
                    "coffee maker",
                    "espresso",
                    "coffee station",
                    "coffee")),
        };
    }

    private static WorkspaceComponentExpansion ForAccessory(
        WorkspaceComponentExpansion componentExpansion,
        string query,
        params string[] keywords)
        => keywords.Any(keyword => query.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            ? componentExpansion
            : WithoutRelevance(componentExpansion);

    private static WorkspaceComponentExpansion WithoutRelevance(WorkspaceComponentExpansion componentExpansion)
        => componentExpansion with
        {
            IsRelevant = false,
            RetrievalQuery = string.Empty,
            SearchTerms = [],
            Synonyms = [],
            SemanticConcepts = [],
            MonthlyBudget = componentExpansion.MonthlyBudget with
            {
                MaximumAmount = null,
                IsExplicit = false,
                IsDerived = false,
            },
        };
}
