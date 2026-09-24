namespace WorkspaceSuggestions.Tools;

/// <summary>Where the catalogue's tools are, and who this agent is when it calls them.</summary>
/// <remarks>
/// <para>
/// Every value is required together or the tools are absent - the shape the deployment settings already use,
/// where a half-configured capability is off rather than half-open.
/// </para>
/// <para>
/// <b>The secret is used to acquire a tokenService and nothing else.</b> It is read here, sent only to the tokenService
/// endpoint, and never logged or echoed in a refusal, so no error line can carry it.
/// </para>
/// </remarks>
internal sealed record CatalogToolSettings(
    string McpEndpoint,
    string TokenEndpoint,
    string Audience,
    string ClientId,
    string ClientSecret)
{
    /// <summary>True when this deployment has been told where the catalogue is and who it is.</summary>
    public bool IsConfigured
        => !string.IsNullOrWhiteSpace(McpEndpoint)
            && !string.IsNullOrWhiteSpace(TokenEndpoint)
            && !string.IsNullOrWhiteSpace(Audience)
            && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret);

    /// <summary>The settings a deployment supplied, or the empty ones that leave the tools off.</summary>
    public static CatalogToolSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new CatalogToolSettings(
            configuration["CatalogTools:McpEndpoint"]?.Trim() ?? string.Empty,
            configuration["CatalogTools:TokenEndpoint"]?.Trim() ?? string.Empty,
            configuration["CatalogTools:Audience"]?.Trim() ?? string.Empty,
            configuration["CatalogTools:ClientId"]?.Trim() ?? string.Empty,
            configuration["CatalogTools:ClientSecret"] ?? string.Empty);
    }
}
