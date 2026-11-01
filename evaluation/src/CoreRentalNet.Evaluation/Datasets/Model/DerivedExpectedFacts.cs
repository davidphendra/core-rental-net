using System.Globalization;

using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
namespace CoreRentalNet.Evaluation.Datasets.Model;

/// <summary>Turns the equipment annotation into the sentences a completeness evaluator needs.</summary>
/// <remarks>
/// Derived facts state quantities only. They make Completeness measurable on every case today without pretending
/// to encode qualifiers such as "27-inch" that only a human annotation can supply.
/// </remarks>
public static class DerivedExpectedFacts
{
    public static IReadOnlyList<string> From(EquipmentExpectation equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);

        var facts = new List<string>(7)
        {
            Describe(equipment.Desks, "desk"),
            Describe(equipment.Chairs, "chair"),
        };

        AddOptional(facts, equipment.Monitors, "monitor");
        AddOptional(facts, equipment.Lamps, "lamp");
        AddOptional(facts, equipment.Plants, "plant");
        AddOptional(facts, equipment.BeanBags, "bean bag");
        AddOptional(facts, equipment.CoffeeMachines, "coffee machine");

        return facts;
    }

    private static void AddOptional(List<string> facts, int quantity, string noun)
    {
        if (quantity > 0)
        {
            facts.Add(Describe(quantity, noun));
        }
    }

    private static string Describe(int quantity, string noun)
    {
        var number = quantity switch
        {
            0 => "no",
            1 => "one",
            _ => quantity.ToString(CultureInfo.InvariantCulture),
        };

        var subject = quantity == 1 ? noun : noun + "s";
        return string.Create(
            CultureInfo.InvariantCulture,
            $"The workspace contains {number} {subject}.");
    }
}
