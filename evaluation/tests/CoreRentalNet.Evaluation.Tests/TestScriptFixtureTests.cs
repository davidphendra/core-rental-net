using AwesomeAssertions;
using CoreRentalNet.Evaluation.Cases;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests;

/// <summary>The sentence and the expected product must describe the same thing.</summary>
public sealed class TestScriptFixtureTests
{

    [Theory]
    [InlineData("TS-017", "ALFA FURNISHING white standing desk", "DSKB0BCJFQHQ")]
    [InlineData("TS-025", "Oklahoma Sound mobile desk", "DSKB07GR57QN")]
    [InlineData("TS-033", "POWERSTONE black standing desk", "DSKB09HHNGMY")]
    [InlineData("TS-057", "VIVO mobile standing desk", "DSKB0BFT5Y9K")]
    [InlineData("TS-065", "Larksilk Boxed Succulent Assortment", "PLTB09K242BX")]
    [InlineData("TS-073", "3 Pack Mini Potted Fake Plants", "PLTB0BS12RH8")]
    public void A_repaired_sentence_identifies_its_expected_product(
        string caseId,
        string sentenceFragment,
        string expectedSku)
    {
        var testCase = Case(caseId);

        testCase.Sentence.Should().Contain(sentenceFragment);
        testCase.Variants.Should().ContainSingle();
        testCase.Variants.Single().Skus.Should().Contain(expectedSku);
    }


    [Theory]
    [InlineData("TS-009", "Dell 3007WFP-HC 30-inch monitor", "MONB000PB2KW")]
    [InlineData("TS-089", "CIMOO black leather chair", "CHAB0BM998N7")]
    [InlineData("TS-161", "CIMOO black leather chair", "CHAB0BM998N7")]
    public void A_repaired_sentence_names_the_product_brand(
        string caseId,
        string sentenceFragment,
        string expectedSku)
    {
        var testCase = Case(caseId);

        testCase.Sentence.Should().Contain(sentenceFragment);
        testCase.Variants.Single().Skus.Should().Contain(expectedSku);
    }

    [Fact]
    public void Ts041_asks_for_the_dracaena_that_is_expected()
    {
        var testCase = Case("TS-041");

        testCase.Sentence.Should().Contain("Dracaena plant");
        Skus(testCase, EvaluationSlot.Plant).Should().ContainSingle().Which.Should().Be("PLTB015HHXT9");
    }

    [Fact]
    public void Ts121_asks_for_the_monstera_that_is_expected()
    {
        var testCase = Case("TS-121");

        testCase.Sentence.Should().Contain("Monstera plant");
        Skus(testCase, EvaluationSlot.Plant).Should().ContainSingle().Which.Should().Be("PLTB0BJDQ4R5");
        Skus(testCase, EvaluationSlot.Chair).Should().Contain("CHAB09RLYPZ1");
    }

    [Fact]
    public void Ts161_asks_for_the_desk_and_chair_that_are_expected()
    {
        var testCase = Case("TS-161");

        testCase.Sentence.Should().Contain("grey standing desk");
        testCase.Sentence.Should().Contain("black leather chair");
        Skus(testCase, EvaluationSlot.Desk).Should().Contain("DSKB08R35RW3");
        Skus(testCase, EvaluationSlot.Chair).Should().Contain("CHAB0BM998N7");
    }

    [Fact]
    public void The_fixture_does_not_use_a_variety_the_catalogue_does_not_have()
    {
        foreach (var testCase in TestScriptManifest.Cases)
        {
            testCase.Sentence.Should().NotContain("snake plant", testCase.CaseId);
            testCase.Sentence.Should().NotContain("bamboo plant", testCase.CaseId);
            testCase.Sentence.Should().NotContain("money plant", testCase.CaseId);
            testCase.Sentence.Should().NotContain("racing chair", testCase.CaseId);
            testCase.Sentence.Should().NotContain("white mesh chair", testCase.CaseId);
        }
    }

    [Fact]
    public void Ts001_and_Ts081_do_not_expect_unrequested_components()
    {
        Case("TS-001").RequestedSlots.Should().BeEquivalentTo([EvaluationSlot.Desk, EvaluationSlot.Chair]);
        Case("TS-081").RequestedSlots.Should().BeEquivalentTo(
            [EvaluationSlot.Desk, EvaluationSlot.Chair, EvaluationSlot.Plant]);
    }

    private static EvaluationCase Case(string caseId)
        => TestScriptManifest.Cases.Single(testCase => testCase.CaseId == caseId);

    private static IReadOnlyList<string> Skus(EvaluationCase testCase, string slot)
        => testCase.Variants.Single().Lines.Single(line => line.Slot == slot).Skus;
}
