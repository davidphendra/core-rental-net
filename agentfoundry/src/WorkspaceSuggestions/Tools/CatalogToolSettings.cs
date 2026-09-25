namespace WorkspaceSuggestions.Tools;

/// <summary>Where the catalogue's MCP tools are.</summary>
/// <remarks>
/// <para>
/// The endpoint is the whole of it. There is no client id, no secret, no token endpoint and no audience: the
/// credential presented to the catalogue is the caller's own token, carried in the request for the call, so
/// the process holds no identity of its own to configure.
/// </para>
/// <para>
/// An unconfigured deployment has an agent with no tools rather than a broken one, exactly as before.
/// </para>
/// </remarks>
internal sealed record CatalogToolSettings(string McpEndpoint)
{
    /// <summary>True when this deployment has been told where the catalogue is.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(McpEndpoint);

    /// <summary>Where the catalogue is, from configuration; empty when this deployment has none.</summary>
    public static CatalogToolSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new CatalogToolSettings(configuration["CatalogTools:McpEndpoint"]?.Trim() ?? string.Empty);
    }
}
