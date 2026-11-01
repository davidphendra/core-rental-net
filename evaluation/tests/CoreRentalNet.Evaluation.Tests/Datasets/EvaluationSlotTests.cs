using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Datasets;

/// <summary>Every canonical slot and every wire alias must resolve to one label.</summary>
public sealed class EvaluationSlotTests
{
    [Theory]
    [InlineData("Desk", "Desk")]
    [InlineData("Chair", "Chair")]
    [InlineData("Monitor", "Monitor")]
    [InlineData("Monitors", "Monitor")]
    [InlineData("Lamp", "Lamp")]
    [InlineData("Plant", "Plant")]
    [InlineData("CoffeeStation", "CoffeeStation")]
    [InlineData("Coffee Machine", "CoffeeStation")]
    [InlineData("RelaxZone", "RelaxZone")]
    [InlineData("Bean Bag", "RelaxZone")]
    public void Canonical_labels_and_aliases_map_to_the_same_slot(string label, string expected)
        => EvaluationSlot.Canonicalize(label).Should().Be(expected);
}
