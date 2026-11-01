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

/// <summary>A status the vocabulary does not recognise is reported as indeterminate, never guessed.</summary>
public sealed class OutcomeExtractorTests
{
    [Theory]
    [InlineData("success", RagOutcome.Success)]
    [InlineData("rejected", RagOutcome.Rejected)]
    [InlineData("unavailable", RagOutcome.Unavailable)]
    [InlineData("  Success  ", RagOutcome.Success)]
    [InlineData("SUCCESS", RagOutcome.Success)]
    public void A_known_status_maps_to_its_outcome(string status, RagOutcome expected)
        => OutcomeExtractor.From(status).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("missing")]
    [InlineData("error")]
    [InlineData("failed")]
    public void An_absent_or_unknown_status_is_indeterminate(string? status)
    {
        OutcomeExtractor.From(status).Should().Be(RagOutcome.Indeterminate);
        OutcomeExtractor.IsRecognised(status).Should().BeFalse();
    }
}
