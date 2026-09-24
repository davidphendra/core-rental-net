using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using WorkspaceSuggestions.Tools;
using Xunit;

namespace WorkspaceSuggestions.Tests;

/// <summary>
/// An unconfigured catalogue leaves the agent with no tools rather than a broken one.
/// </summary>
/// <remarks>
/// Every catalogue setting is required together, so a deployment that has set none of them has a working agent
/// that cannot search - the same "absent means off" shape the other settings use - and nothing to dispose.
/// </remarks>
public sealed class McpCatalogToolsTests
{
    [Fact]
    public async Task Without_configuration_there_are_no_tools_and_nothing_to_dispose()
    {
        var settings = CatalogToolSettings.From(new ConfigurationBuilder().Build());

        await using var catalogue = await McpCatalogTools.ConnectAsync(settings, new StubCatalogAccessTokenService());

        catalogue.Tools.Should().BeEmpty();
    }

    [Fact]
    public async Task A_partly_configured_catalogue_is_still_off()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CatalogTools:McpEndpoint"] = "https://catalogue.example/mcp",
                ["CatalogTools:Audience"] = "https://catalogue.example",
            })
            .Build();

        await using var catalogue = await McpCatalogTools.ConnectAsync(
            CatalogToolSettings.From(configuration),
            new StubCatalogAccessTokenService());

        catalogue.Tools.Should().BeEmpty("a half-configured capability is off, not half-open");
    }
}
