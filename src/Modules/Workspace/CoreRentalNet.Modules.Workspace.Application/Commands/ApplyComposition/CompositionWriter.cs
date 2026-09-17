using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Commands.ApplyComposition;

/// <summary>
/// Turns a composition into the assignments a workspace should hold, or refuses it.
/// </summary>
/// <remarks>
/// <para>
/// Every line is built before any of them is written, because a composition is one answer rather than a
/// list of independent ones: a low-tier desk with a high-tier chair is a workspace nobody chose, and the
/// option the customer accepted would have described something else. Building first is what makes a
/// rejected line leave the workspace exactly as it was instead of half-replaced.
/// </para>
/// <para>
/// It lives beside the operation rather than inside the workspace service because the service is already
/// the longest thing in this module, and because this is a rule about compositions rather than about
/// workspaces: the service can assign a product to a slot, and this is the thing that knows what a whole
/// composition has to be.
/// </para>
/// </remarks>
internal sealed class CompositionWriter(ISlotRuleProvider slotRules)
{
    public IReadOnlyList<SlotAssignment> Assignments(IReadOnlyList<CompositionLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var assignments = lines.Select(AssignmentFor).ToList();

        EnsureEachSlotOnce(assignments);

        return assignments;
    }

    /// <summary>One line, as an assignment, refused if the slot cannot hold it.</summary>
    private SlotAssignment AssignmentFor(CompositionLine line)
    {
        if (line.Quantity < 1)
        {
            throw new DomainRuleViolationException($"Quantity must be at least 1, but was {line.Quantity}.");
        }

        var slot = SlotNamed(line.Slot);
        var rule = slotRules.For(slot);

        if (line.Quantity > rule.MaxQuantity)
        {
            throw new DomainRuleViolationException(
                $"The {rule.DisplayName} slot holds at most {rule.MaxQuantity} units, but the composition asks for {line.Quantity}.");
        }

        return new SlotAssignment
        {
            Slot = slot,
            Sku = Guard.NotEmpty(line.Sku, "SKU", 32).ToUpperInvariant(),
            Quantity = line.Quantity,
        };
    }

    /// <summary>Refuses a composition that names a slot twice.</summary>
    /// <remarks>
    /// A tier means desk, chair and monitor all chosen together, so a composition proposing two monitors
    /// is not a tier - and allowing it would put the slot's own maximum out of reach of the check above,
    /// which sees one line at a time.
    /// </remarks>
    private static void EnsureEachSlotOnce(IReadOnlyList<SlotAssignment> assignments)
    {
        var seen = new HashSet<SlotId>();

        foreach (var assignment in assignments)
        {
            if (!seen.Add(assignment.Slot))
            {
                throw new DomainRuleViolationException(
                    $"A composition names the {assignment.Slot} slot more than once, so it is not one answer.");
            }
        }
    }

    /// <summary>The slot a composition named, or a refusal naming what it said.</summary>
    private static SlotId SlotNamed(string slot)
        => Enum.TryParse<SlotId>(slot, ignoreCase: true, out var parsed)
            ? parsed
            : throw new DomainRuleViolationException($"There is no '{slot}' slot in a workspace.");
}
