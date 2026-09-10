namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>
/// The seven positions a workspace can fill. A slot is derived from what a product is,
/// never chosen by the customer, so placing a plant in the chair slot is not something the
/// code can express.
/// </summary>
public enum SlotId
{
    Desk = 1,
    Chair = 2,
    Monitor = 3,
    Lamp = 4,
    Plant = 5,
    CoffeeStation = 6,
    RelaxZone = 7,
}
