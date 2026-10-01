using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Where an MCP server is, reached as the caller rather than as the process.</summary>
/// <remarks>
/// The endpoint is the whole of it, because the credential is the caller's own token carried in the request: the
/// process holds no identity of its own to configure. One instance per server a feature uses, so the
/// configuration key that names it belongs to the feature and not to this type.
/// </remarks>
public sealed record McpSetting(string McpEndpoint)
{
    /// <summary>True when this deployment has been told where the server is.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(McpEndpoint);

    /// <summary>The endpoint, read from the key the feature declares for it.</summary>
    public static McpSetting FromConfiguration(
        IConfiguration configuration, string configurationKey)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var mcpEndpoint = configuration[configurationKey]?.Trim() ?? string.Empty;

        // Fail at startup rather than at the first search: an endpoint that is not an absolute URI cannot be
        // reached, and a run that discovers that has already been paid for. An empty value stays legal, because a
        // deployment with no catalogue is a deployment that publishes no tools.
        if (mcpEndpoint.Length > 0 && !IsAnHttpEndpoint(mcpEndpoint))
        {
            throw new InvalidOperationException(
                $"'{configurationKey}' is set to '{mcpEndpoint}', which is not an absolute http or https " +
                "endpoint. It must be one, or empty when this deployment has no catalogue.");
        }

        return new McpSetting(mcpEndpoint);
    }

    /// <summary>Whether the configured endpoint is one an MCP client can be pointed at.</summary>
    /// <remarks>
    /// <b>Checked positively, because the URI parser is generous.</b> <c>Uri.TryCreate</c> accepts
    /// <c>localhost:5502</c> as an absolute URI whose scheme is <c>localhost</c> and whose path is <c>5502</c>, so
    /// "it parsed" says nothing about whether a transport could reach it. What the schema, the host and the
    /// transport all require is an http or https endpoint with a host in it.
    /// </remarks>
    private static bool IsAnHttpEndpoint(string mcpEndpoint)
        => Uri.TryCreate(mcpEndpoint, UriKind.Absolute, out var endpoint)
            && (endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps)
            && endpoint.Host.Length > 0;
}
