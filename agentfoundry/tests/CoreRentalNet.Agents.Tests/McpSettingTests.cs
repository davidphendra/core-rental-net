using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The catalogue endpoint is checked when it is read, which is at startup and not at the first search.</summary>
/// <remarks>
/// <b>A deployment that names an endpoint it cannot reach should fail where somebody is watching.</b> The first
/// search happens inside a paid-for run, and a run whose catalogue is unreachable is reported to the customer as
/// an unavailable suggestion rather than as the misconfiguration it is.
/// </remarks>
public sealed class McpSettingTests
{
    private const string TheEndpointKey = "CatalogTools:McpEndpoint";

    [Fact]
    public void An_absolute_endpoint_is_accepted()
        => SettingsFor("https://catalogue.example/mcp").McpEndpoint.Should().Be("https://catalogue.example/mcp");

    [Fact] // a deployment with no catalogue publishes no tools, which is a configuration rather than a fault
    public void An_endpoint_that_is_not_configured_is_accepted_and_reads_as_unconfigured()
    {
        var settings = SettingsFor(string.Empty);

        settings.IsConfigured.Should().BeFalse();
    }

    [Theory]
    [InlineData("catalogue.example/mcp")]      // a path, not an endpoint
    [InlineData("/mcp")]                       // a path on whatever host this happens to be
    [InlineData("localhost:5502")]             // parses as scheme "localhost" and path "5502"
    [InlineData("ftp://catalogue.example/mcp")] // an endpoint, over a transport an MCP client cannot use
    public void An_endpoint_that_is_not_an_absolute_uri_is_refused_where_somebody_is_watching(string configuredEndpoint)
    {
        var act = () => SettingsFor(configuredEndpoint);

        act.Should().Throw<InvalidOperationException>(
                "an endpoint that cannot be reached must not wait for a paid-for run to be discovered")
            .WithMessage("*not an absolute http or https endpoint*");
    }

    private static McpSetting SettingsFor(string configuredEndpoint)
        => McpSetting.FromConfiguration(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [TheEndpointKey] = configuredEndpoint })
                .Build(),
            TheEndpointKey);
}
