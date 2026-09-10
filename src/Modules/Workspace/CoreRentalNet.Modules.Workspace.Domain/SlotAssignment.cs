using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>One slot holding a number of units of a single product. Holds no price.</summary>
public sealed record SlotAssignment
{
    public SlotAssignment(SlotId slot, string sku, int quantity)
    {
        var maxQuantity = SlotRules.For(slot).MaxQuantity;

        if (quantity < 1 || quantity > maxQuantity)
        {
            throw new DomainRuleViolationException(
                $"{slot} holds between 1 and {maxQuantity} units, but {quantity} was requested.");
        }

        Slot = slot;
        Sku = Guard.NotEmpty(sku, "SKU", 32).ToUpperInvariant();
        Quantity = quantity;
    }

    public SlotId Slot { get; }

    public string Sku { get; }

    public int Quantity { get; }
}
