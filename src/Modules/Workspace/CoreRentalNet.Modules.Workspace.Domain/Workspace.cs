using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>
/// The customer's draft: what each slot holds and where it should be delivered.
/// </summary>
/// <remarks>
/// This is the authority for what the workspace contains. Every total shown to the customer
/// is computed from here, on the server, from the catalog — the client never sends a price
/// or a total (ADR-0006).
/// </remarks>
public sealed class Workspace
{
    public const int MinDeliveryAddressLength = 5;
    public const int MaxDeliveryAddressLength = 200;

    private readonly List<SlotAssignment> assignments = [];

    private Workspace()
    {
        DraftTokenHash = string.Empty;
    }

    private Workspace(WorkspaceId id, string draftTokenHash)
    {
        Id = WorkspaceId.From(id.Value);
        DraftTokenHash = Guard.NotEmpty(draftTokenHash, "Draft token hash", 128);
        State = DraftState.Draft;
        Version = 1;
    }

    public WorkspaceId Id { get; private set; }

    public string DraftTokenHash { get; private set; }

    public DraftState State { get; private set; }

    /// <summary>
    /// Bumped by every mutation and mapped as a concurrency token, so two tabs editing the
    /// same draft produce a conflict rather than a silent lost update (ADR-0003).
    /// </summary>
    public int Version { get; private set; }

    public string? DeliveryAddress { get; private set; }

    public IReadOnlyList<SlotAssignment> Assignments => assignments;

    public bool IsConverted => State == DraftState.Converted;

    public bool IsEmpty => assignments.Count == 0;

    public int TotalUnits => assignments.Sum(assignment => assignment.Quantity);

    public static Workspace CreateNew(WorkspaceId id, string draftTokenHash) => new(id, draftTokenHash);

    /// <summary>Everything a slot holds, in the order it was added.</summary>
    public IReadOnlyList<SlotAssignment> AssignmentsFor(SlotId slot)
        => [.. assignments.Where(assignment => assignment.Slot == slot)];

    /// <summary>Whether every slot that must hold something does.</summary>
    public bool HasEveryMandatorySlot => SlotRules.Mandatory.All(rule => AssignmentsFor(rule.Slot).Count > 0);

    /// <summary>
    /// Adds units of a product to a slot.
    /// </summary>
    /// <remarks>
    /// A slot that holds one thing holds one thing, so choosing another replaces it: that is what
    /// makes the desk, the chair and the four single-unit zones behave as pickers rather than stacks.
    ///
    /// A slot that holds several holds them side by side. The same product adds units; a different
    /// product is added beside it rather than replacing what is already there; and the slot's
    /// capacity is the total it will accept, however it is divided (matrix WS-14).
    /// </remarks>
    public void Assign(SlotId slot, string sku, int quantity = 1)
    {
        EnsureNotConverted();

        if (quantity < 1)
        {
            throw new DomainRuleViolationException($"Quantity must be at least 1, but was {quantity}.");
        }

        var rule = SlotRules.For(slot);
        var normalizedSku = Guard.NotEmpty(sku, "SKU", 32).ToUpperInvariant();

        if (rule.MaxQuantity == 1)
        {
            assignments.RemoveAll(assignment => assignment.Slot == slot);
            assignments.Add(new SlotAssignment(slot, normalizedSku, 1));
            Touch();
            return;
        }

        var existing = Find(slot, normalizedSku);
        var wanted = (existing?.Quantity ?? 0) + quantity;

        // What the slot holds is what the message says it holds: counting only the other products
        // reported "2 are already assigned" of a slot already holding three.
        if (HeldIn(slot) + quantity > rule.MaxQuantity)
        {
            throw new DomainRuleViolationException(
                $"The {rule.DisplayName} slot holds at most {rule.MaxQuantity} units, and already holds {HeldIn(slot)}.");
        }

        if (existing is not null)
        {
            assignments.Remove(existing);
        }

        assignments.Add(new SlotAssignment(slot, normalizedSku, wanted));
        Touch();
    }

    /// <summary>Empties a slot. Returns false when the slot was already empty.</summary>
    public bool Remove(SlotId slot)
    {
        EnsureNotConverted();

        if (assignments.RemoveAll(assignment => assignment.Slot == slot) == 0)
        {
            return false;
        }

        Touch();
        return true;
    }

    /// <summary>Removes one product from a slot, leaving the rest. False when it was not there.</summary>
    public bool Remove(SlotId slot, string sku)
    {
        EnsureNotConverted();

        var normalizedSku = Guard.NotEmpty(sku, "SKU", 32).ToUpperInvariant();

        if (assignments.RemoveAll(assignment => assignment.Slot == slot && assignment.Sku == normalizedSku) == 0)
        {
            return false;
        }

        Touch();
        return true;
    }

    /// <summary>Sets how many of one product a slot holds. Zero removes that product.</summary>
    public void ChangeQuantity(SlotId slot, string sku, int quantity)
    {
        EnsureNotConverted();

        if (quantity < 0)
        {
            throw new DomainRuleViolationException($"Quantity cannot be negative, but was {quantity}.");
        }

        var rule = SlotRules.For(slot);
        var normalizedSku = Guard.NotEmpty(sku, "SKU", 32).ToUpperInvariant();
        var existing = Find(slot, normalizedSku)
            ?? throw new DomainRuleViolationException($"The {rule.DisplayName} slot does not hold {normalizedSku}, so its quantity cannot change.");

        if (quantity == 0)
        {
            assignments.Remove(existing);
            Touch();
            return;
        }

        var others = HeldIn(slot) - existing.Quantity;

        if (others + quantity > rule.MaxQuantity)
        {
            throw new DomainRuleViolationException(
                $"The {rule.DisplayName} slot holds at most {rule.MaxQuantity} units, but {others + quantity} were requested.");
        }

        assignments.Remove(existing);
        assignments.Add(new SlotAssignment(slot, normalizedSku, quantity));
        Touch();
    }

    /// <summary>
    /// Records where the setup should go. An empty value clears it; a value that is too short
    /// is refused rather than stored (ADR-0008, matrix ADDR-02/03).
    /// </summary>
    public void SetDeliveryAddress(string? address)
    {
        EnsureNotConverted();

        var normalized = NormalizeAddress(address);

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

        DeliveryAddress = normalized;
        Touch();
    }

    public void MarkConverted()
    {
        if (IsConverted)
        {
            throw new DomainRuleViolationException("This workspace has already been turned into an order.");
        }

        State = DraftState.Converted;
        Touch();
    }

    /// <summary>Trims each line, drops blank ones and normalises line endings.</summary>
    private static string? NormalizeAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var lines = address
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);

        var normalized = string.Join('\n', lines).Trim();

        return normalized.Length == 0 ? null : normalized;
    }

    private void EnsureNotConverted()
    {
        if (IsConverted)
        {
            throw new DomainRuleViolationException("A workspace that has been turned into an order can no longer be changed.");
        }
    }

    private SlotAssignment? Find(SlotId slot, string sku)
    {
        foreach (var assignment in assignments)
        {
            if (assignment.Slot == slot && assignment.Sku == sku)
            {
                return assignment;
            }
        }

        return null;
    }

    private int HeldIn(SlotId slot)
    {
        var held = 0;

        foreach (var assignment in assignments)
        {
            if (assignment.Slot == slot)
            {
                held += assignment.Quantity;
            }
        }

        return held;
    }

    private void Touch() => Version++;
}
