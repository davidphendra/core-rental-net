using System.Reflection;
using AwesomeAssertions;
using CoreRentalNet.Host.Presentation;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// Where the footer's version comes from.
/// </summary>
/// <remarks>
/// The host had no unit tests until this rule needed one. The browser suite starts the application
/// as a process and cannot see how it was stamped, and the version is stamped in
/// Directory.Build.props, so the rule that turns a stamp into something to display is checked here.
/// </remarks>
public sealed class AppVersionTests
{
    [Theory] // VER-01
    [InlineData("0.0.1", "0.0.1")]
    [InlineData("1.4.2", "1.4.2")]
    [InlineData("v1.4.2", "1.4.2")] // a pipeline passing the tag straight through
    [InlineData("1.4.2-rc.1", "1.4.2-rc.1")] // a pre-release label is part of a semantic version
    [InlineData("1.4.2+729b11c", "1.4.2")] // the SDK's commit hash is not part of the version
    [InlineData("1.4.2-rc.1+729b11c", "1.4.2-rc.1")]
    [InlineData("  1.4.2  ", "1.4.2")]
    [InlineData("", "0.0.1")]
    [InlineData("   ", "0.0.1")]
    [InlineData(null, "0.0.1")]
    [InlineData("release-7", "0.0.1")] // refused rather than shown
    [InlineData("1.4", "0.0.1")]
    [InlineData("1.4.2.0", "0.0.1")]
    public void A_version_is_shown_only_when_it_is_a_semantic_version(string? stamped, string shown)
        => AppVersion.Parse(stamped).Should().Be(shown);

    [Fact] // VER-02
    public void The_application_is_stamped_with_a_version_and_not_a_build_fingerprint()
    {
        var stamped = typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        // Turning this off is what keeps the suffix out of the assembly in the first place, rather
        // than relying on the display to trim it back off. Removing it from Directory.Build.props
        // fails here.
        stamped.Should().NotBeNull().And.NotContain("+", "the commit hash is not part of a version");
        AppVersion.Current.Should().MatchRegex(@"^\d+\.\d+\.\d+$");
    }
}
