using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class EvaluationCaseSetSelectorTests
{
    [Fact]
    public void Tiny_selects_five_evenly_spaced_rows()
    {
        var selected = Select(EvaluationScope.Tiny);

        selected.Cases.Select(testCase => testCase.RowNumber)
            .Should().Equal(1, 41, 81, 121, 161);
    }

    [Fact]
    public void Small_selects_twenty_five_rows()
    {
        var selected = Select(EvaluationScope.Small);

        selected.Cases.Should().HaveCount(25);
        selected.Cases[0].RowNumber.Should().Be(1);
        selected.Cases[^1].RowNumber.Should().Be(193);
    }

    [Fact]
    public void Half_selects_every_other_row()
    {
        var selected = Select(EvaluationScope.Half);

        selected.Cases.Should().HaveCount(100);
        selected.Cases.Select(testCase => testCase.RowNumber)
            .Should().Equal(Enumerable.Range(0, 100).Select(index => index * 2 + 1));
    }

    [Fact]
    public void All_preserves_every_row()
    {
        var selected = Select(EvaluationScope.All);

        selected.Cases.Should().HaveCount(200);
        selected.Cases[0].RowNumber.Should().Be(1);
        selected.Cases[^1].RowNumber.Should().Be(200);
    }

    [Fact]
    public void Selection_preserves_the_script_order_without_duplicates()
    {
        var selected = Select(EvaluationScope.Small);

        selected.Cases.Select(testCase => testCase.RowNumber)
            .Should().BeInAscendingOrder()
            .And.OnlyHaveUniqueItems();
    }

    private static EvaluationCaseSet Select(EvaluationScope scope)
        => EvaluationCaseSetSelector.Select(TestScriptManifest.LoadedSet.Cases, scope);
}
