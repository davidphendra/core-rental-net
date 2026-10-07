using AwesomeAssertions;
using CoreRentalNet.Host.Presentation;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>The build identity the footer shows and the telemetry resource carries. The record is the policy.</summary>
public sealed class AppBuildIdentityTests
{
    [Fact] // VER-08
    public void A_build_that_named_no_environment_reports_the_local_default()
    {
        var identity = AppBuildIdentity.Create("1.4.1", null, null, null);

        identity.Environment.Should().Be(AppBuildIdentity.LocalEnvironment);
        identity.Label.Should().Be("1.4.1 · local");
    }

    [Fact] // VER-08
    public void The_resource_attributes_omit_what_the_build_did_not_carry()
    {
        // A developer's build stamps none of the three, so the resource carries only what it can prove. An
        // empty attribute would be a dimension every item shares and no one can act on.
        var keys = AppBuildIdentity.Create("1.4.1", null, null, "production")
            .ResourceAttributes()
            .Select(attribute => attribute.Key);

        keys.Should().Equal("version", "environment");
    }

    [Fact] // VER-08
    public void The_resource_attributes_are_the_four_every_dashboard_slices_by()
        => AppBuildIdentity.Create("1.4.1", "abc1234", "12345", "production")
            .ResourceAttributes()
            .Should().BeEquivalentTo(
            [
                new KeyValuePair<string, object>("version", "1.4.1"),
                new KeyValuePair<string, object>("environment", "production"),
                new KeyValuePair<string, object>("gitSha", "abc1234"),
                new KeyValuePair<string, object>("buildId", "12345"),
            ]);

    [Fact] // VER-08
    public void The_label_is_what_then_where_then_which_build()
        => AppBuildIdentity.Create("1.4.1", "abc1234", "12345", "production")
            .Label.Should().Be("1.4.1 · production · abc1234 · 12345");

    [Fact] // VER-08
    public void A_stamped_but_blank_value_is_not_an_identity()
    {
        // The pipeline omits what it does not have, but a value that arrives empty must not become a field
        // the footer prints as nothing.
        var identity = AppBuildIdentity.Create("1.4.1", "  ", "", "  ");

        identity.GitSha.Should().BeNull();
        identity.BuildId.Should().BeNull();
        identity.Environment.Should().Be(AppBuildIdentity.LocalEnvironment);
    }
}
