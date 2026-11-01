using AwesomeAssertions;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
using Xunit;

namespace CoreRentalNet.Evaluation.Tests.Evidence;

public sealed class ServerTimingTests
{
    [Fact]
    public void The_named_durations_are_read_from_the_header()
    {
        var values = ServerTiming.Parse("mcp;dur=842, embedding;dur=320, vector-search;dur=280");

        values["mcp"].Should().Be(842);
        values["embedding"].Should().Be(320);
        values["vector-search"].Should().Be(280);
    }

    [Fact]
    public void An_absent_header_is_an_empty_breakdown()
    {
        ServerTiming.Parse(null).Should().BeEmpty();
    }
}
