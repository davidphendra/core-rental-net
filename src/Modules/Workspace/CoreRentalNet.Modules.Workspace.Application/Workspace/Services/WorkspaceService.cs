using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Rules;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Support;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Workspace.Services;

/// <summary>
/// Everything that used to live on the <c>Workspace</c> aggregate, now in a service.
/// </summary>
/// <remarks>
/// The rules are unchanged, and so are the refusal messages: the behaviour lock in
/// <c>CoreRentalNet.BehaviourLock</c> pins them. What changed is where they are enforced — the record
/// carries data, this service carries the rules.
/// </remarks>
public sealed class WorkspaceService(ISlotRuleProvider slotRules) : IWorkspaceService
{
    private const int MinDeliveryAddressLength = 5;
    private const int MaxDeliveryAddressLength = 200;

    /// <summary>
    /// Adds units of a product to a slot.
    /// </summary>
    /// <remarks>
    /// A slot that holds one thing holds one thing, so choosing another replaces it: that is what
    /// makes the desk, the chair and the four single-unit zones behave as pickers rather than stacks.
    /// A slot that holds several holds them side by side. The same product adds units; a different
    /// product is added beside it; and the slot's capacity is the total it will accept, however it is
    /// divided (matrix WS-14).
    /// </remarks>
    public void Assign(Domain.Workspace workspace, SlotId slot, string sku, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        EnsureNotConverted(workspace);

        if (quantity < 1)
        {
            throw new DomainRuleViolationException($"Quantity must be at least 1, but was {quantity}.");
        }

        var rule = slotRules.For(slot);
        var normalizedSku = Guard.NotEmpty(sku, "SKU", 32).ToUpperInvariant();

        if (rule.MaxQuantity == 1)
        {
            workspace.Assignments.RemoveAll(assignment => assignment.Slot == slot);
            workspace.Assignments.Add(new SlotAssignment { Slot = slot, Sku = normalizedSku, Quantity = 1 });
            Touch(workspace);
            return;
        }

        var existing = Find(workspace, slot, normalizedSku);
        var wanted = (existing?.Quantity ?? 0) + quantity;

        // What the slot holds is what the message says it holds: counting only the other products
        // reported "2 are already assigned" of a slot already holding three.
        if (HeldIn(workspace, slot) + quantity > rule.MaxQuantity)
        {
            throw new DomainRuleViolationException(
                $"The {rule.DisplayName} slot holds at most {rule.MaxQuantity} units, and already holds {HeldIn(workspace, slot)}.");
        }

        if (existing is not null)
        {
            workspace.Assignments.Remove(existing);
        }

        workspace.Assignments.Add(new SlotAssignment { Slot = slot, Sku = normalizedSku, Quantity = wanted });
        Touch(workspace);
    }

    public bool Remove(Domain.Workspace workspace, SlotId slot)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        EnsureNotConverted(workspace);

        if (workspace.Assignments.RemoveAll(assignment => assignment.Slot == slot) == 0)
        {
            return false;
        }

        Touch(workspace);
        return true;
    }

    public void ChangeQuantity(Domain.Workspace workspace, SlotId slot, string sku, int quantity)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        EnsureNotConverted(workspace);

        if (quantity < 0)
        {
            throw new DomainRuleViolationException($"Quantity cannot be negative, but was {quantity}.");
        }

        var rule = slotRules.For(slot);
        var normalizedSku = Guard.NotEmpty(sku, "SKU", 32).ToUpperInvariant();
        var existing = Find(workspace, slot, normalizedSku)
            ?? throw new DomainRuleViolationException($"The {rule.DisplayName} slot does not hold {normalizedSku}, so its quantity cannot change.");

        if (quantity == 0)
        {
            workspace.Assignments.Remove(existing);
            Touch(workspace);
            return;
        }

        var others = HeldIn(workspace, slot) - existing.Quantity;

        if (others + quantity > rule.MaxQuantity)
        {
            throw new DomainRuleViolationException(
                $"The {rule.DisplayName} slot holds at most {rule.MaxQuantity} units, but {others + quantity} were requested.");
        }

        workspace.Assignments.Remove(existing);
        workspace.Assignments.Add(new SlotAssignment { Slot = slot, Sku = normalizedSku, Quantity = quantity });
        Touch(workspace);
    }

    /// <summary>
    /// Records where the setup should go. An empty value clears it; a value that is too short
    /// is refused rather than stored.
    /// </summary>
    public void SetDeliveryAddress(Domain.Workspace workspace, string? address)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        EnsureNotConverted(workspace);

        var normalized = DeliveryAddressNormalizer.Normalize(address);

        if (normalized is not null && normalized.Length < MinDeliveryAddressLength)
        {
            throw new DomainRuleViolationException(
                $"A delivery address needs at least {MinDeliveryAddressLength} characters.");
        }

        if (normalized is not null && normalized.Length > MaxDeliveryAddressLength)
        {
            throw new DomainRuleViolationException(
                $"A delivery address cannot be longer than {MaxDeliveryAddressLength} characters.");
        }

        workspace.DeliveryAddress = normalized;
        Touch(workspace);
    }

    public void MarkConverted(Domain.Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        if (workspace.State == DraftState.Converted)
        {
            throw new DomainRuleViolationException("This workspace has already been turned into an order.");
        }

        workspace.State = DraftState.Converted;
        Touch(workspace);
    }

    /// <summary>Trims each line, drops blank ones and normalises line endings.</summary>
    private static void EnsureNotConverted(Domain.Workspace workspace)
    {
        if (workspace.State == DraftState.Converted)
        {
            throw new DomainRuleViolationException("A workspace that has been turned into an order can no longer be changed.");
        }
    }

    private static SlotAssignment? Find(Domain.Workspace workspace, SlotId slot, string sku)
    {
        foreach (var assignment in workspace.Assignments)
        {
            if (assignment.Slot == slot && assignment.Sku == sku)
            {
                return assignment;
            }
        }

        return null;
    }

    private static int HeldIn(Domain.Workspace workspace, SlotId slot)
    {
        var held = 0;

        foreach (var assignment in workspace.Assignments)
        {
            if (assignment.Slot == slot)
            {
                held += assignment.Quantity;
            }
        }

        return held;
    }

    private static void Touch(Domain.Workspace workspace) => workspace.Version++;
}
