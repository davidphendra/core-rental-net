using AwesomeAssertions;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.Composition;
using CoreRentalNet.Modules.Discovery.Application.Indexing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// What start-up does when the catalogue index is not the one this deployment would build.
/// </summary>
/// <remarks>
/// The decision taken for this epic was to <b>hide the feature rather than refuse to start</b>: the storefront,
/// the catalogue and the orders are not hostage to an optional feature's derived data. These tests hold both
/// halves of that — the feature really is hidden, and the application really does keep going.
/// </remarks>
public sealed class DiscoveryUnavailableTests
{
    private static DiscoverySettings Configured()
        => new("https://example.services.ai.azure.com/api/projects/demo", "embeddings", "text-embedding-3-large", 512, 2, 0.1, 30);

    private static DiscoverySettings Unconfigured()
        => new(string.Empty, string.Empty, string.Empty, 0, 2, 0.1, 30);

    /// <summary>An application with just enough registered to run the gate, and a log a test can read.</summary>
    private static (WebApplication App, StubCatalogIndexFreshness Freshness, CapturingLoggerProvider Logs) Host(
        DiscoverySettings settings,
        StubCatalogIndexFreshness freshness)
    {
        var builder = WebApplication.CreateBuilder();
        var logs = new CapturingLoggerProvider();

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(new SuggestionAvailability());
        builder.Services.AddSingleton<ICatalogIndexFreshness>(freshness);
        builder.Services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(logging => logging.AddProvider(logs)));

        return (builder.Build(), freshness, logs);
    }

    [Fact] // SCR-20
    public async Task An_index_that_was_never_built_hides_the_feature_and_names_the_tool()
    {
        var (app, _, logs) = Host(Configured(), new StubCatalogIndexFreshness(() => CatalogIndexVerdict.NotBuilt));
        await using var host = app;

        await app.GateSuggestionOnIndexAsync();

        app.Services.GetRequiredService<SuggestionAvailability>().IsAvailable.Should().BeFalse();
        logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Error && entry.Message.Contains("hidden"));
        logs.Entries.Should().Contain(entry => entry.Message.Contains("CatalogIngestion"));
    }

    [Fact] // SCR-20
    public async Task An_index_that_is_current_hides_nothing()
    {
        var (app, freshness, logs) = Host(Configured(), new StubCatalogIndexFreshness(() => CatalogIndexVerdict.Usable));
        await using var host = app;

        await app.GateSuggestionOnIndexAsync();

        freshness.Checks.Should().Be(1, "the check has to have run for this to prove anything");
        app.Services.GetRequiredService<SuggestionAvailability>().IsAvailable.Should().BeTrue();
        logs.Entries.Should().NotContain(entry => entry.Message.Contains("hidden"));
    }

    [Fact] // SCR-20
    public async Task A_stale_index_says_which_of_the_recorded_values_moved()
    {
        var (app, _, logs) = Host(
            Configured(),
            new StubCatalogIndexFreshness(() => CatalogIndexVerdict.Stale("the catalogue has changed since the index was built")));
        await using var host = app;

        await app.GateSuggestionOnIndexAsync();

        // The operator has to be able to act on the line, so it says which value disagreed rather than only that
        // something did.
        app.Services.GetRequiredService<SuggestionAvailability>().Reason.Should().Contain("catalogue has changed");
        logs.Entries.Should().Contain(entry => entry.Message.Contains("catalogue has changed"));
    }

    [Fact] // SCR-20
    public async Task The_application_starts_even_when_the_check_itself_fails()
    {
        // The safe direction at start-up: hiding costs a feature, and throwing costs the whole application. A
        // genuine defect is still visible in the log rather than silent.
        var (app, _, logs) = Host(
            Configured(),
            new StubCatalogIndexFreshness(() => throw new InvalidOperationException("the database is unreadable")));
        await using var host = app;

        var gate = async () => await app.GateSuggestionOnIndexAsync();

        await gate.Should().NotThrowAsync();
        app.Services.GetRequiredService<SuggestionAvailability>().IsAvailable.Should().BeFalse();
        logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Error && entry.Message.Contains("could not be checked"));
    }

    [Fact] // SCR-20
    public async Task Nothing_is_checked_when_the_deployment_has_not_been_told_where_to_embed()
    {
        // The feature is already hidden for want of configuration, and the embedding client registered is the one
        // that refuses. Reading the index anyway would be a database read on every start-up for a deployment that
        // cannot use it.
        var (app, freshness, _) = Host(Unconfigured(), new StubCatalogIndexFreshness(() => CatalogIndexVerdict.NotBuilt));
        await using var host = app;

        await app.GateSuggestionOnIndexAsync();

        freshness.Checks.Should().Be(0);
        app.Services.GetRequiredService<SuggestionAvailability>().IsAvailable.Should().BeTrue();
    }
}
