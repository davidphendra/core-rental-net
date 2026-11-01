using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using Xunit;
using AwesomeAssertions;

namespace CoreRentalNet.Evaluation.Tests.Datasets;

/// <summary>A two-pack monitors SKU is two monitors, so quantities cannot be counted as SKUs.</summary>
public sealed class SlotQuantitiesTests
{
    private static readonly ProductRecord[] Products =
    [
        new("MONB086Q776F", "Lenovo D24-10 2-Pack", "accessory", "monitor", 1m),
        new("MONB09GS97F5", "Acer Nitro QG271", "accessory", "monitor", 1m),
        new("DSKB08XN4JDR", "HON Mod Desk Shell", "desk", null, 1m),
        new("CHAB09RLYPZ1", "WorkPro Oceanic Mesh Chair", "chair", null, 1m),
    ];

    [Fact]
    public void A_two_pack_counts_as_two_units()
    {
        var variant = Variant((EvaluationSlot.Monitor, ["MONB086Q776F", "MONB09GS97F5"]));

        var equipment = SlotQuantities.From(variant, Products);

        equipment.Monitors.Should().Be(3);
    }

    [Fact]
    public void Single_products_count_once_each()
    {
        var variant = Variant(
            (EvaluationSlot.Desk, ["DSKB08XN4JDR"]),
            (EvaluationSlot.Chair, ["CHAB09RLYPZ1"]),
            (EvaluationSlot.Monitor, ["MONB09GS97F5"]));

        var equipment = SlotQuantities.From(variant, Products);

        equipment.Desks.Should().Be(1);
        equipment.Chairs.Should().Be(1);
        equipment.Monitors.Should().Be(1);
        equipment.SatisfiesMandatoryConstraints.Should().BeTrue();
    }

    [Fact]
    public void A_pack_size_is_read_from_the_catalogue_name_only()
    {
        SlotQuantities.PackSize("Lenovo D24-10 2-Pack").Should().Be(2);
        SlotQuantities.PackSize("Acer Nitro QG271").Should().Be(1);
    }

    private static ExpectedSetupVariant Variant(params (string Slot, string[] Skus)[] lines)
        => new([.. lines.Select(line => new ExpectedLine(line.Slot, line.Skus))]);
}
