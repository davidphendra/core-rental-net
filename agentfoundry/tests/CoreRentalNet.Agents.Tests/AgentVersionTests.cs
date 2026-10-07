using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Diagnostics;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>Where the agent's reported version and identity come from, and that it is read rather than invented.</summary>
public sealed class AgentVersionTests
{
    [Theory] // AGENT-VER-01
    [InlineData("0.1.0", "0.1.0")]
    [InlineData("v1.4.2", "1.4.2")]
    [InlineData("1.4.2-rc.1", "1.4.2-rc.1")]
    [InlineData("1.4.2+abc1234", "1.4.2")]
    [InlineData("1.4.2-rc.1+abc1234", "1.4.2-rc.1")]
    [InlineData("release-7", "0.1.0")] // refused rather than shown
    [InlineData("1.4", "0.1.0")]
    [InlineData(null, "0.1.0")]
    [InlineData("", "0.1.0")]
    public void A_version_is_shown_only_when_it_is_a_semantic_version(string? stamped, string shown)
        => AgentVersion.Parse(stamped).Should().Be(shown);

    [Fact] // AGENT-VER-02
    public void A_metadata_value_is_read_by_key_and_an_absent_one_is_nothing()
    {
        // Proven against this assembly rather than the agent's, so the test does not depend on whether the
        // pipeline stamped the agent it happens to be running beside.
        var assembly = typeof(AgentVersionTests).Assembly;

        AgentVersion.MetadataOf(assembly, "BuildIdentityTestKey").Should().Be("stamped-value");
        AgentVersion.MetadataOf(assembly, "BuildIdentityTestBlankKey").Should().BeNull();
        AgentVersion.MetadataOf(assembly, "BuildIdentityTestMissingKey").Should().BeNull();
    }

    [Fact] // AGENT-VER-03
    public void The_assembly_is_stamped_with_a_version()
    {
        // The pipeline, or the generated Version.g.props, supplies the release; a developer's machine supplies
        // the floor. Either way what is shown is a version.
        AgentVersion.Current.Should().MatchRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$");
    }
}
