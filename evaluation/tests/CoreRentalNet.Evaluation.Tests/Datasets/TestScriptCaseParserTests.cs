using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Datasets;

/// <summary>The fixture is 200 test cases, not 59 unique setups.</summary>
public sealed class TestScriptCaseParserTests
{
    [Fact]
    public void The_script_parses_into_200_cases()
    {
        var loaded = Load();

        loaded.Cases.Cases.Should().HaveCount(200);
        loaded.Cases.UniqueSentenceCount.Should().Be(58);
        loaded.Cases.UniqueSetupCount.Should().Be(56);
    }

    [Fact]
    public void Row_24_resolves_both_named_monitors_to_skus()
    {
        var row = Load().Cases.Cases.Single(testCase => testCase.RowNumber == 24);

        row.Variants.Single().Lines
            .Single(line => line.Slot == EvaluationSlot.Monitor)
            .Skus.Should().Equal("MONB09GS97F5", "MONB086Q776F");
    }

    [Fact]
    public void Rows_13_and_37_keep_both_expected_variants()
    {
        var loaded = Load();
        var first = loaded.Cases.Cases.Single(testCase => testCase.RowNumber == 13);
        var second = loaded.Cases.Cases.Single(testCase => testCase.RowNumber == 37);

        first.Sentence.Should().Be(second.Sentence);
        first.Variants.Should().HaveCount(2);
        second.Variants.Should().HaveCount(2);
        first.AmbiguityGroup.Should().NotBeNull();
    }

    [Fact]
    public void Exact_duplicate_rows_keep_their_own_case_and_a_duplicate_group()
    {
        var loaded = Load();
        var first = loaded.Cases.Cases.Single(testCase => testCase.RowNumber == 19);
        var second = loaded.Cases.Cases.Single(testCase => testCase.RowNumber == 59);

        first.CaseId.Should().NotBe(second.CaseId);
        first.DuplicateGroup.Should().NotBeNull().And.Be(second.DuplicateGroup);
    }

    [Fact]
    public void The_fixture_passes_validation()
    {
        Load().Validation.IsValid.Should().BeTrue(
            string.Join(Environment.NewLine, Load().Validation.Errors));
    }

    private static LoadedCaseSet Load()
        => CaseSetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Test_Script.md"),
            Path.Combine(AppContext.BaseDirectory, "products.json"));
}
