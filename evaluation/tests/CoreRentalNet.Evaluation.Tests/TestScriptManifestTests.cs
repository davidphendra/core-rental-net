using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

public sealed class TestScriptManifestTests
{
    [Fact]
    public void Exactly_200_cases_are_present()
    {
        TestScriptManifest.Cases.Should().HaveCount(200);
    }

    [Fact]
    public void Every_case_id_is_unique()
    {
        TestScriptManifest.Cases
            .Select(testCase => testCase.CaseId)
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Every_row_in_the_markdown_is_a_case()
    {
        var markdownRows = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Test_Script.md"))
            .Count(line => line.StartsWith("| ") && char.IsDigit(line.AsSpan(2, 1)[0]));

        TestScriptManifest.Cases.Should().HaveCount(markdownRows);
    }
}
