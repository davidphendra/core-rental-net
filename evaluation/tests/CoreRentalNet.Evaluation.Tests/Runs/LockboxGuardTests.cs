using AwesomeAssertions;
using CoreRentalNet.Evaluation.Datasets.Model;
using CoreRentalNet.Evaluation.Datasets.Loading;
using CoreRentalNet.Evaluation.Datasets.Validation;
using CoreRentalNet.Evaluation.Runs.Record;
using CoreRentalNet.Evaluation.Runs.Storage;
using CoreRentalNet.Evaluation.Runs.Result;
using CoreRentalNet.Evaluation.Runs.Assembly;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Runs;

/// <summary>The lockbox is refused before a case runs, not labelled afterwards.</summary>
public sealed class LockboxGuardTests
{
    [Fact]
    public void A_development_or_validation_run_needs_no_confirmation()
        => LockboxGuard.Violations(Dataset(DatasetName.Validation), false, false, []).Should().BeEmpty();

    [Fact]
    public void The_test_dataset_is_refused_without_the_confirmation_flag()
    {
        var violations = LockboxGuard.Violations(Dataset(DatasetName.Test), false, false, []);

        violations.Should().ContainSingle().Which.Should().Contain("--confirm-lockbox");
    }

    [Fact]
    public void A_confirmed_first_run_is_allowed()
        => LockboxGuard.Violations(Dataset(DatasetName.Test), true, false, ["other-hash"]).Should().BeEmpty();

    [Fact]
    public void The_same_hash_may_not_be_evaluated_twice_by_accident()
    {
        var violations = LockboxGuard.Violations(Dataset(DatasetName.Test), true, false, ["lockbox-hash"]);

        violations.Should().ContainSingle().Which.Should().Contain("--re-evaluate-lockbox");
    }

    [Fact]
    public void A_deliberate_re_evaluation_is_allowed_and_named_as_such()
        => LockboxGuard.Violations(Dataset(DatasetName.Test), true, true, ["lockbox-hash"]).Should().BeEmpty();

    private static RagEvaluationDataset Dataset(DatasetName name) => new(
        name, "memory", "v1", "lockbox-hash", 0, [], []);
}
