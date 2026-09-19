using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Services;

/// <summary>Replaces a draft's whole composition, or replaces nothing at all.</summary>
public sealed class WorkspaceCompositionService(ISlotRuleProvider slotRules) : IWorkspaceCompositionService
{
    /// <remarks>
    /// <para>
    /// <b>The version is checked before anything is written.</b> The caller read a version, decided what to
    /// put in its place, and came back - and if the draft moved in between, the replacement is refused rather
    /// than merged. Merging would silently discard whatever the customer did while the suggestion was being
    /// drawn, which is the one thing they would never think to check.
    /// </para>
    /// <para>
    /// <b>The address is not part of a composition, so a replacement cannot change it.</b> A customer who
    /// asked for a different setup did not ask to be re-addressed. Nothing here mentions the address, which is
    /// the mechanism; a test states it as a contract.
    /// </para>
    /// <para>
    /// Nothing is written until every line has been checked, so a composition that does not fit leaves the
    /// draft holding what it held. The version is a concurrency token as well, so a write that races this one
    /// is refused by the database rather than by a check that could be stale by the time it runs.
    /// </para>
    /// </remarks>
    public void ReplaceComposition(
        Domain.Workspace workspace,
        IReadOnlyList<SlotAssignment> composition,
        int expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(composition);

        if (workspace.State == DraftState.Converted)
        {
            throw new DomainRuleViolationException(
                "A workspace that has been turned into an order can no longer be changed.");
        }

        if (workspace.Version != expectedVersion)
        {
            throw new DomainRuleViolationException(
                "This workspace changed while the suggestion was being put together, so nothing was applied.");
        }

        var replacement = Fit(composition);

        workspace.Assignments.Clear();
        workspace.Assignments.AddRange(replacement);

        // A forgotten bump is a silent lost update, so every accepted change makes it here.
        workspace.Version++;
    }

    /// <summary>
    /// The composition as the draft should store it, or nothing at all when a line of it cannot be honoured.
    /// </summary>
    /// <remarks>
    /// Built into fresh assignments rather than written over the caller's, so a refused replacement cannot
    /// leave a half-normalised line behind in something the caller still holds.
    /// </remarks>
    private List<SlotAssignment> Fit(IReadOnlyList<SlotAssignment> composition)
    {
        var replacement = new List<SlotAssignment>(composition.Count);
        var held = new Dictionary<SlotId, int>();

        foreach (var line in composition)
        {
            if (line.Quantity < 1)
            {
                throw new DomainRuleViolationException($"Quantity must be at least 1, but was {line.Quantity}.");
            }

            var rule = slotRules.For(line.Slot);
            var total = held.GetValueOrDefault(line.Slot) + line.Quantity;

            if (total > rule.MaxQuantity)
            {
                throw new DomainRuleViolationException(
                    $"The {rule.DisplayName} slot holds at most {rule.MaxQuantity} units, but {total} were replaced in.");
            }

            held[line.Slot] = total;

            replacement.Add(new SlotAssignment
            {
                Slot = line.Slot,
                Sku = Guard.NotEmpty(line.Sku, "SKU", 32).ToUpperInvariant(),
                Quantity = line.Quantity,
            });
        }

        return replacement;
    }
}
