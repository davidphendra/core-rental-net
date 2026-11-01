using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;

namespace CoreRentalNet.Evaluation.Datasets.Model;

/// <summary>The equipment quantities an evaluation case expects, in the vocabulary the rules are stated in.</summary>
public sealed record EquipmentExpectation(
    int Desks,
    int Chairs,
    int Monitors,
    int Lamps,
    int Plants,
    int BeanBags,
    int CoffeeMachines)
{
    /// <summary>Whether the quantities satisfy every mandatory workspace constraint.</summary>
    public bool SatisfiesMandatoryConstraints =>
        Desks == 1
        && Chairs == 1
        && Monitors is >= 0 and <= 3
        && Lamps is >= 0 and <= 1
        && Plants is >= 0 and <= 1
        && BeanBags is >= 0 and <= 1
        && CoffeeMachines is >= 0 and <= 1;

    /// <summary>The number of units requested across all seven slots.</summary>
    public int TotalUnits => Desks + Chairs + Monitors + Lamps + Plants + BeanBags + CoffeeMachines;
}
