using System.ClientModel.Primitives;

namespace CoreRentalNet.IntegrationTests;

/// <summary>The end of the pipeline, so a test's message stops where a socket would have started.</summary>
/// <remarks>
/// It stands where the transport stands and deliberately does not pass the message on: the policy under test is
/// then the last thing that ran, which is the position it holds in production.
/// </remarks>
internal sealed class TheTransportThatNeverRuns : PipelinePolicy
{
    public override void Process(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
    {
        // Nothing to send. The assertions are over the message the policy built.
    }

    public override ValueTask ProcessAsync(
        PipelineMessage message,
        IReadOnlyList<PipelinePolicy> pipeline,
        int currentIndex)
        => default;
}
