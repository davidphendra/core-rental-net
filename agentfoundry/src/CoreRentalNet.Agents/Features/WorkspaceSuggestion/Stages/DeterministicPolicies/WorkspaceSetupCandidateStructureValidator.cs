using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Answers whether a composed setup is structurally possible, before a model is asked to judge it.</summary>
/// <remarks>
/// Deterministic, and deliberately cheap: a setup naming a product no search returned, or more of it than a slot
/// holds, is not a candidate a reviewer should spend a call on. What survives here is a setup the catalogue can
/// actually honour, and only then is the reviewer asked whether it satisfies the request.
/// </remarks>
public sealed class WorkspaceSetupCandidateStructureValidator
{
    /// <summary>True when every line is grounded in the retrieved set and within the slot capacities.</summary>
    public bool IsStructurallyValid(
        WorkspaceSetupCandidate workspaceSetupCandidate,
        WorkspaceRequirementExpansion requirementExpansion,
        IReadOnlyList<SlotRule> slotCapacityRules,
        IReadOnlyList<SelectedWorkspaceComponentProduct> selectedWorkspaceCandidates,
        out IReadOnlyList<string> violations)
    {
        ArgumentNullException.ThrowIfNull(workspaceSetupCandidate);
        ArgumentNullException.ThrowIfNull(requirementExpansion);

        var foundViolations = new List<string>();

        if (workspaceSetupCandidate.Lines.Count == 0)
        {
            foundViolations.Add("SETUP_HAS_NO_LINES");
        }

        // The rephraser answers in categories and a setup line is written by slot, so what "this slot was asked
        // for" means is asked of the mapping rather than of the document.
        var requestedSlots = requirementExpansion.ComponentExpansions.Every()
            .Where(component => component.Expansion.IsRelevant)
            .Select(component => WorkspaceComponentVocabularyMapping.CompositionSlotFor(component.ComponentCategory))
            .ToHashSet();
        var capacities = slotCapacityRules.ToDictionary(rule => rule.Slot, rule => rule.Capacity);
        // The boundary is what the composer was GIVEN, not what a search returned: a product the reranker
        // dropped was rejected on purpose, and letting a composed line name it would undo that judgement.
        var retrievedStockKeepingUnits = selectedWorkspaceCandidates
            .Select(selection => (selection.RetrievedProduct.Slot, selection.RetrievedProduct.Sku))
            .ToHashSet();

        foreach (var workspaceSetupLine in workspaceSetupCandidate.Lines)
        {
            if (!requestedSlots.Contains(workspaceSetupLine.Slot))
            {
                foundViolations.Add($"SLOT_NOT_REQUESTED:{workspaceSetupLine.Slot}");
            }

            if (!retrievedStockKeepingUnits.Contains((workspaceSetupLine.Slot, workspaceSetupLine.Sku)))
            {
                foundViolations.Add($"SKU_NOT_RETRIEVED:{workspaceSetupLine.Sku}");
            }

            if (capacities.TryGetValue(workspaceSetupLine.Slot, out var capacity)
                && (workspaceSetupLine.Quantity < 1 || workspaceSetupLine.Quantity > capacity))
            {
                foundViolations.Add($"QUANTITY_ABOVE_CAPACITY:{workspaceSetupLine.Slot}");
            }
        }

        violations = foundViolations;

        return foundViolations.Count == 0;
    }
}
