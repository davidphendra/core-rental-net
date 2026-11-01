using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

namespace CoreRentalNet.Evaluation.Cases.Model;

/// <summary>The seven composition slots, in one place for parsing, constraints and reporting.</summary>
public static class EvaluationSlot
{
    public const string Desk = "Desk";
    public const string Chair = "Chair";
    public const string Monitor = "Monitor";
    public const string Lamp = "Lamp";
    public const string Plant = "Plant";
    public const string CoffeeStation = "CoffeeStation";
    public const string RelaxZone = "RelaxZone";

    public static string Canonicalize(string label)
        => label.Trim() switch
        {
            Desk => Desk,
            Chair => Chair,
            "Monitor" or "Monitors" => Monitor,
            Lamp => Lamp,
            Plant => Plant,
            CoffeeStation => CoffeeStation,
            "Coffee Machine" => CoffeeStation,
            RelaxZone => RelaxZone,
            "Bean Bag" => RelaxZone,
            var unknown => throw new InvalidOperationException($"Unknown workspace slot '{unknown}'."),
        };
}
