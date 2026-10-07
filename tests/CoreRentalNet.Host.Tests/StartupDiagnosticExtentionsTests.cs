using AwesomeAssertions;
using CoreRentalNet.Host.Extentions;
using CoreRentalNet.Host.Presentation;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// What a deployment prints about itself at start-up.
/// </summary>
/// <remarks>
/// Proven by what was logged rather than by the strings the method builds: the point of the announcement is
/// that an operator reading the log stream can name the artifact, so the logged text is the fact.
/// </remarks>
public sealed class StartupDiagnosticExtentionsTests
{
    [Fact] // VER-09
    public void The_announcement_names_the_build_and_where_it_runs()
    {
        var logger = new FakeLogger();

        StartupDiagnosticExtentions.Announce(
            logger,
            AppBuildIdentity.Create("1.4.1", "abc1234", "12345", "production"));

        logger.Collector.GetSnapshot().Select(record => record.Message).Should().Equal(
            "[startup] core-rental-app",
            "[startup]   version     : 1.4.1",
            "[startup]   environment : production",
            "[startup]   git sha     : abc1234",
            "[startup]   build id    : 12345");
    }

    [Fact] // VER-09
    public void A_field_the_build_did_not_carry_is_shown_rather_than_omitted()
    {
        // A developer's build stamps neither the commit nor the run, and the announcement has to say so: a
        // missing field is what an operator is looking for when a deployment reports the wrong build.
        var logger = new FakeLogger();

        StartupDiagnosticExtentions.Announce(logger, AppBuildIdentity.Create("1.4.1", null, null, null));

        var messages = logger.Collector.GetSnapshot().Select(record => record.Message);

        messages.Should().Equal(
            "[startup] core-rental-app",
            "[startup]   version     : 1.4.1",
            "[startup]   environment : local",
            "[startup]   git sha     : (unset)",
            "[startup]   build id    : (unset)");
    }
}
