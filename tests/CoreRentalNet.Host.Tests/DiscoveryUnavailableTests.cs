using AwesomeAssertions;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Extentions;
using CoreRentalNet.Modules.Discovery.Application.Indexing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// What start-up does when the stored vectors are not the ones this deployment would build.
/// </summary>
/// <remarks>
/// The decision taken for this epic was to <b>refuse the feature rather than refuse to start</b>: the storefront,
/// the catalogue and the orders are not hostage to an optional feature's derived data. These tests hold both
/// halves of that — the search really is refused, and the application really does keep going.
/// </remarks>
public sealed class DiscoveryUnavailableTests
{
    private static VectorEmbeddingSettings Configured()
        => new("https://example.openai.azure.com/", "key", "text-embedding-3-small", "App_Data/product_embedding.db", 384);

    private static VectorEmbeddingSettings Unconfigured()
        => new(string.Empty, string.Empty, string.Empty, string.Empty, 384);

    /// <summary>An application with just enough registered to run the gate, and a log a test can read.</summary>
    private static (WebApplication App, StubCatalogIndexFreshnessService Freshness, CapturingLoggerProvider Logs) Host(
        VectorEmbeddingSettings settings,
        StubCatalogIndexFreshnessService freshnessService)
    {
        var builder = WebApplication.CreateBuilder();
        var logs = new CapturingLoggerProvider();

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(new CatalogIndexAvailability());
        builder.Services.AddSingleton<ICatalogIndexFreshnessService>(freshnessService);
        builder.Services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(logging => logging.AddProvider(logs)));

        return (builder.Build(), freshnessService, logs);
    }

    [Fact] // SCR-20
    public void Vectors_that_were_never_built_refuse_the_search_and_name_the_tool()
    {
        var (app, _, logs) = Host(Configured(), new StubCatalogIndexFreshnessService(() => CatalogIndexVerdict.NotBuilt));
        using var host = app;

        app.CheckSimilarityOnVectorEmbedding();

        app.Services.GetRequiredService<CatalogIndexAvailability>().IsUsable.Should().BeFalse();
        logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Error && entry.Message.Contains("unavailable"));
        logs.Entries.Should().Contain(entry => entry.Message.Contains("CatalogIngestion"));
    }

    [Fact] // SCR-20
    public void Vectors_that_are_current_refuse_nothing()
    {
        var (app, freshness, logs) = Host(Configured(), new StubCatalogIndexFreshnessService(() => CatalogIndexVerdict.Usable));
        using var host = app;

        app.CheckSimilarityOnVectorEmbedding();

        freshness.Checks.Should().Be(1, "the check has to have run for this to prove anything");
        app.Services.GetRequiredService<CatalogIndexAvailability>().IsUsable.Should().BeTrue();
        logs.Entries.Should().NotContain(entry => entry.Message.Contains("unavailable"));
    }

    [Fact] // SCR-20
    public void Stale_vectors_say_which_of_the_recorded_values_moved()
    {
        var (app, _, logs) = Host(
            Configured(),
            new StubCatalogIndexFreshnessService(() => CatalogIndexVerdict.Stale("the catalogue has changed since the vectors were built")));
        using var host = app;

        app.CheckSimilarityOnVectorEmbedding();

        // The operator has to be able to act on the line, so it says which value disagreed rather than only that
        // something did.
        app.Services.GetRequiredService<CatalogIndexAvailability>().Reason.Should().Contain("catalogue has changed");
        logs.Entries.Should().Contain(entry => entry.Message.Contains("catalogue has changed"));
    }

    [Fact] // SCR-20
    public async Task The_application_starts_even_when_the_check_itself_fails()
    {
        // The safe direction at start-up: refusing costs a feature, and throwing costs the whole application. A
        // genuine defect is still visible in the log rather than silent.
        var (app, _, logs) = Host(
            Configured(),
            new StubCatalogIndexFreshnessService(() => throw new InvalidOperationException("the database is unreadable")));
        await using var host = app;

        var gate = () => app.CheckSimilarityOnVectorEmbedding();

        gate.Should().NotThrow();
        app.Services.GetRequiredService<CatalogIndexAvailability>().IsUsable.Should().BeFalse();
        logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Error && entry.Message.Contains("could not be checked"));
    }

    [Fact] // SCR-20
    public void Nothing_is_checked_when_the_deployment_has_not_been_told_where_to_embed()
    {
        // The feature is already unavailable for want of configuration, and the embedding client registered is the
        // one that refuses. Reading the vectors anyway would be a file read on every start-up for a deployment that
        // cannot use it.
        var (app, freshness, _) = Host(Unconfigured(), new StubCatalogIndexFreshnessService(() => CatalogIndexVerdict.NotBuilt));
        using var host = app;

        app.CheckSimilarityOnVectorEmbedding();

        freshness.Checks.Should().Be(0);
        app.Services.GetRequiredService<CatalogIndexAvailability>().IsUsable.Should().BeTrue();
    }
}
