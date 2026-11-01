using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using Xunit;
using AwesomeAssertions;

namespace CoreRentalNet.Evaluation.Tests.Datasets;

/// <summary>Derived facts state quantities in the words a reader expects, and stay silent about absent slots.</summary>
public sealed class DerivedExpectedFactsTests
{
    [Fact]
    public void Mandatory_slots_always_produce_a_sentence()
    {
        var facts = DerivedExpectedFacts.From(new EquipmentExpectation(1, 1, 0, 0, 0, 0, 0));

        facts.Should().Equal(
            "The workspace contains one desk.",
            "The workspace contains one chair.");
    }

    [Fact]
    public void Optional_slots_appear_only_when_requested_and_count_in_plural()
    {
        var facts = DerivedExpectedFacts.From(new EquipmentExpectation(1, 1, 2, 2, 0, 1, 0));

        facts.Should().Contain("The workspace contains 2 monitors.");
        facts.Should().Contain("The workspace contains 2 lamps.");
        facts.Should().Contain("The workspace contains one bean bag.");
        facts.Should().NotContain(fact => fact.Contains("plant"));
    }

    [Fact]
    public void An_annotated_case_keeps_its_own_facts()
    {
        var testCase = new RagEvaluationCase
        {
            Id = "VAL-001",
            ExpectedFacts = ["The monitor must be 27 inches."],
        };

        testCase.Facts.Source.Should().Be(ExpectedFactsSource.Annotated);
        testCase.Facts.Facts.Should().Equal("The monitor must be 27 inches.");
    }
}
