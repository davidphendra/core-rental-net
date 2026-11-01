using System.Text.RegularExpressions;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Validation;
namespace CoreRentalNet.Evaluation.Datasets.Loading;

/// <summary>Turns an expected slot/SKU set into equipment quantities.</summary>
/// <remarks>
/// One SKU is not always one unit: several monitors ship as a two-pack, so the quantity of a line is the sum of
/// each SKU's pack size rather than the number of SKUs. The pack size is read from the catalogue name, which is
/// the only place it is stated.
/// </remarks>
internal static partial class SlotQuantities
{
    private static readonly Regex PackPattern = new(
        @"\b(?:2[\s-]?pack|2[\s-]?pk|set of 2|pair of)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static EquipmentExpectation From(
        ExpectedSetupVariant variant,
        IReadOnlyList<ProductRecord> products)
    {
        ArgumentNullException.ThrowIfNull(variant);
        ArgumentNullException.ThrowIfNull(products);

        var bySku = products.ToDictionary(product => product.SkuNo, StringComparer.Ordinal);

        return new EquipmentExpectation(
            Desks: Units(variant, EvaluationSlot.Desk, bySku),
            Chairs: Units(variant, EvaluationSlot.Chair, bySku),
            Monitors: Units(variant, EvaluationSlot.Monitor, bySku),
            Lamps: Units(variant, EvaluationSlot.Lamp, bySku),
            Plants: Units(variant, EvaluationSlot.Plant, bySku),
            BeanBags: Units(variant, EvaluationSlot.RelaxZone, bySku),
            CoffeeMachines: Units(variant, EvaluationSlot.CoffeeStation, bySku));
    }

    /// <summary>The pack size a catalogue name states, or one when it states none.</summary>
    public static int PackSize(string catalogueName)
        => PackPattern.IsMatch(catalogueName) ? 2 : 1;

    private static int Units(
        ExpectedSetupVariant variant,
        string slot,
        IReadOnlyDictionary<string, ProductRecord> bySku)
        => variant.SkusFor(slot).Sum(sku => bySku.TryGetValue(sku, out var product) ? PackSize(product.Name) : 1);
}
