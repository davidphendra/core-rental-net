using AwesomeAssertions;
using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Helpers;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The line a catalogue request writes: the caller, the filters it asked for, and what came back.
/// </summary>
public sealed class CatalogApiLogHelperTests
{
    [Fact] // API-12
    public void The_line_carries_the_caller_the_filters_and_the_count()
    {
        var provider = new CapturingLoggerProvider();
        var logger = provider.CreateLogger(CatalogApiLogHelper.Category);

        CatalogApiLogHelper.Called(logger, "client-1", "desk", "monitor", "teak", 3);

        var entry = provider.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Property("Caller").Should().Be("client-1");
        entry.Property("Category").Should().Be("desk");
        entry.Property("SubCategory").Should().Be("monitor");
        entry.Property("Search").Should().Be("teak");
        entry.Property("Count").Should().Be(3);
    }

    [Fact] // API-12
    public void An_absent_filter_is_recorded_as_null_not_as_a_word()
    {
        var provider = new CapturingLoggerProvider();

        CatalogApiLogHelper.Called(provider.CreateLogger(CatalogApiLogHelper.Category), "anonymous", null, null, null, 62);

        var entry = provider.Entries.Should().ContainSingle().Subject;
        entry.Property("Category").Should().BeNull();
        entry.Property("SubCategory").Should().BeNull();
        entry.Property("Search").Should().BeNull();
        entry.Property("Count").Should().Be(62);
    }
}
