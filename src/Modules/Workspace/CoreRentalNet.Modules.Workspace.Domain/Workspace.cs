using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>
/// The customer's draft: which slots are filled, how many units each holds, and where it
/// should be delivered.
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

    public SlotAssignment? AssignmentFor(SlotId slot)
    {
        foreach (var assignment in assignments)
        {
            if (assignment.Slot == slot)
            {
                return assignment;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds units to a slot. A single-capacity slot is replaced; a multi-capacity slot
    /// accumulates and refuses to exceed its capacity rather than silently replacing.
    /// </summary>
    public void Assign(SlotId slot, string sku, int quantity = 1)
    {
        EnsureNotConverted();

        if (quantity < 1)
        {
            throw new DomainRuleViolationException($"Quantity must be at least 1, but was {quantity}.");
        }

        var rule = SlotRules.For(slot);
        var normalizedSku = Guard.NotEmpty(sku, "SKU", 32).ToUpperInvariant();
        var existing = AssignmentFor(slot);

        if (existing is null)
        {
            assignments.Add(new SlotAssignment(slot, normalizedSku, quantity));
            Touch();
            return;
        }

        if (rule.MaxQuantity == 1)
        {
            assignments.Remove(existing);
            assignments.Add(new SlotAssignment(slot, normalizedSku, 1));
            Touch();
            return;
        }

        var wanted = existing.Quantity + quantity;

        if (wanted > rule.MaxQuantity)
        {
            throw new DomainRuleViolationException(
                $"The {rule.DisplayName} slot holds at most {rule.MaxQuantity} units, and {existing.Quantity} {(existing.Quantity == 1 ? "is" : "are")} already assigned.");
        }

        assignments.Remove(existing);
        assignments.Add(new SlotAssignment(slot, normalizedSku, wanted));
        Touch();
    }

    /// <summary>Empties a slot. Returns false when the slot was already empty.</summary>
    public bool Remove(SlotId slot)
    {
        EnsureNotConverted();

        var existing = AssignmentFor(slot);

        if (existing is null)
        {
            return false;
        }

        assignments.Remove(existing);
        Touch();
        return true;
    }

    /// <summary>Sets the quantity of a filled slot. Zero empties it.</summary>
    public void ChangeQuantity(SlotId slot, int quantity)
    {
        EnsureNotConverted();

        if (quantity < 0)
        {
            throw new DomainRuleViolationException($"Quantity cannot be negative, but was {quantity}.");
        }

        var existing = AssignmentFor(slot)
            ?? throw new DomainRuleViolationException($"The {SlotRules.For(slot).DisplayName} slot is empty, so its quantity cannot change.");

        if (quantity == 0)
        {
            assignments.Remove(existing);
            Touch();
            return;
        }

        var rule = SlotRules.For(slot);

        if (quantity > rule.MaxQuantity)
        {
            throw new DomainRuleViolationException(
                $"The {rule.DisplayName} slot holds at most {rule.MaxQuantity} units, but {quantity} was requested.");
        }

        assignments.Remove(existing);
        assignments.Add(new SlotAssignment(slot, existing.Sku, quantity));
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

    private void Touch() => Version++;
}
