using System.Reflection;
using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Diagnostics;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>Where the agent's reported version comes from, and that it is a version and not a fingerprint.</summary>
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

    [Theory] // AGENT-VER-02
    [InlineData("1.4.0+abc1234", "abc1234")]
    [InlineData("1.4.0-rc.1+abc1234", "abc1234")]
    [InlineData("1.4.0", null)]
    [InlineData("1.4.0+", null)]
    [InlineData(null, null)]
    public void The_commit_is_read_from_the_build_metadata(string? stamped, string? build)
        => AgentVersion.ParseBuild(stamped).Should().Be(build);

    [Fact] // AGENT-VER-03
    public void The_assembly_is_stamped_with_a_version_and_an_optional_commit()
    {
        // The pipeline, or the generated Version.g.props, supplies both; a developer's machine supplies
        // the floor and no commit. Either way the version is a version and the commit is read separately.
        var stamped = typeof(AgentVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        AgentVersion.Current.Should().MatchRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$");
        AgentVersion.Build.Should().Be(AgentVersion.ParseBuild(stamped));
    }
}
