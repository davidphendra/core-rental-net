using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

/// <summary>One contract test per row, so the fixture itself is a 200-case suite.</summary>
public sealed class TestCaseContractTests
{
    public static IEnumerable<object[]> Cases
        => TestScriptManifest.Cases.Select(testCase => new object[] { testCase.CaseId });

    [Theory]
    [MemberData(nameof(Cases))]
    public void The_case_has_a_desk_seating_and_catalogue_skus(string caseId)
    {
        var testCase = TestScriptManifest.Cases.Single(candidate => candidate.CaseId == caseId);

        testCase.Variants.Should().NotBeEmpty();
        testCase.RequestedSlots.Should().Contain(EvaluationSlot.Desk);

        (testCase.RequestedSlots.Contains(EvaluationSlot.Chair)
         || testCase.RequestedSlots.Contains(EvaluationSlot.RelaxZone))
            .Should().BeTrue("every workspace needs a place to sit");

        foreach (var sku in testCase.Variants.SelectMany(variant => variant.Skus))
        {
            TestScriptManifest.LoadedSet.Catalogue.Should().ContainKey(sku);
        }
    }
}
