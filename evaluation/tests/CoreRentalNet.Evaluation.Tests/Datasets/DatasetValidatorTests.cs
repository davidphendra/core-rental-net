using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using Xunit;
using AwesomeAssertions;

namespace CoreRentalNet.Evaluation.Tests.Datasets;

/// <summary>A case the rules can never grade is refused before any model call is spent.</summary>
public sealed class DatasetValidatorTests
{
    private static readonly EquipmentExpectation OneDeskOneChair = new(1, 1, 0, 0, 0, 0, 0);

    [Fact]
    public void A_success_case_that_breaks_a_limit_is_invalid()
    {
        var dataset = Dataset(RagOutcome.Success, new EquipmentExpectation(2, 1, 0, 0, 0, 0, 0));

        var validation = new DatasetValidator().Validate(dataset);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle().Which.Should().Contain("breaks a mandatory limit");
    }

    [Fact]
    public void A_rejected_case_that_satisfies_every_limit_is_invalid()
    {
        var dataset = Dataset(RagOutcome.Rejected, OneDeskOneChair);

        var validation = new DatasetValidator().Validate(dataset);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle().Which.Should().Contain("marked rejected");
    }

    [Fact]
    public void An_unavailable_case_must_still_be_a_valid_configuration()
    {
        var dataset = Dataset(RagOutcome.Unavailable, new EquipmentExpectation(1, 0, 0, 0, 0, 0, 0));

        var validation = new DatasetValidator().Validate(dataset);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().ContainSingle().Which.Should().Contain("marked unavailable");
    }

    [Fact]
    public void A_success_case_carrying_no_skus_warns_that_retrieval_is_not_evaluable()
    {
        var dataset = Dataset(RagOutcome.Success, OneDeskOneChair);

        var validation = new DatasetValidator().Validate(dataset);

        validation.IsValid.Should().BeTrue();
        validation.Warnings.Should().Contain(warning => warning.Contains("no relevance labels"));
    }

    [Fact]
    public void The_same_id_in_two_datasets_is_an_error()
    {
        var validation = new DatasetValidator().ValidateAcrossDatasets(
            [Dataset(RagOutcome.Success, OneDeskOneChair), Dataset(RagOutcome.Success, OneDeskOneChair)]);

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(error => error.Contains("unique across datasets"));
    }

    private static RagEvaluationDataset Dataset(RagOutcome outcome, EquipmentExpectation equipment)
        => new(
            Name: DatasetName.Validation,
            Path: "memory",
            Version: "v1",
            Sha256: "0",
            SizeInBytes: 0,
            Cases:
            [
                new RagEvaluationCase
                {
                    Id = "VAL-001",
                    Category = "normal",
                    Query = "one desk and one chair",
                    ExpectedOutcome = outcome,
                    ExpectedEquipment = equipment,
                },
            ],
            Exclusions: []);
}
