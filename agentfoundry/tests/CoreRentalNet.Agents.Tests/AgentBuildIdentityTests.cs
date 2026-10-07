using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Diagnostics;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The build identity's defaults and the resource attributes it contributes.</summary>
/// <remarks>
/// The identity is pure, so these prove the rules without a host, a process environment or an exporter: the
/// environment always has a name, and a value the build did not carry is omitted rather than sent empty.
/// </remarks>
public sealed class AgentBuildIdentityTests
{
    [Theory] // AGENT-VER-05
    [InlineData(null, "local")]
    [InlineData("", "local")]
    [InlineData("   ", "local")]
    [InlineData("production", "production")]
    [InlineData("  production  ", "production")]
    public void An_absent_or_blank_environment_is_local(string? environment, string expected)
        => AgentBuildIdentity.Create("1.4.1", "abc1234", "12345", environment).Environment.Should().Be(expected);

    [Fact] // AGENT-VER-06
    public void The_resource_attributes_omit_what_the_build_did_not_carry()
    {
        var build = AgentBuildIdentity.Create("1.4.1", null, null, "production");

        build.ResourceAttributes().Should().BeEquivalentTo(new[]
        {
            new KeyValuePair<string, object>("version", "1.4.1"),
            new KeyValuePair<string, object>("environment", "production"),
        });
    }

    [Fact] // AGENT-VER-07
    public void The_resource_attributes_are_the_whole_identity_when_the_build_carried_everything()
    {
        var build = AgentBuildIdentity.Create("1.4.1", "abc1234", "12345", "local");

        build.ResourceAttributes().Should().BeEquivalentTo(new[]
        {
            new KeyValuePair<string, object>("version", "1.4.1"),
            new KeyValuePair<string, object>("gitSha", "abc1234"),
            new KeyValuePair<string, object>("buildId", "12345"),
            new KeyValuePair<string, object>("environment", "local"),
        });
    }
}
