namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Which requests the caller's catalogue token may be attached to.</summary>
/// <remarks>
/// <para>
/// The rule is stated here rather than inside the handler so it can be tested without a network: the token goes to
/// <b>the one server it is for</b>, over a transport that protects it.
/// </para>
/// <para>
/// <b>Plain text is permitted only for a loopback endpoint, and only because the local catalogue is served over
/// http.</b> That exception is decided from the configured endpoint rather than from the environment, so a
/// deployment cannot inherit it by being mislabelled.
/// </para>
/// </remarks>
internal sealed class CatalogueServerRequestPolicy
{
    private readonly string _catalogueServerAuthority;
    private readonly bool _plainTextToTheLoopbackIsPermitted;

    public CatalogueServerRequestPolicy(McpSetting mcpSetting)
    {
        ArgumentNullException.ThrowIfNull(mcpSetting);

        var catalogueServerUri = new Uri(mcpSetting.McpEndpoint, UriKind.Absolute);

        _catalogueServerAuthority = catalogueServerUri.GetLeftPart(UriPartial.Authority);
        _plainTextToTheLoopbackIsPermitted =
            catalogueServerUri.Scheme == Uri.UriSchemeHttp
            && catalogueServerUri.IsLoopback;
    }

    /// <summary>Whether this request is one the caller's token was meant for.</summary>
    public bool MayCarryTheCallersCatalogueAccessToken(Uri mcpRequestUri)
    {
        ArgumentNullException.ThrowIfNull(mcpRequestUri);

        return IsAddressedToTheCatalogueServer(mcpRequestUri)
            && UsesATransportThatProtectsTheToken(mcpRequestUri);
    }

    private bool IsAddressedToTheCatalogueServer(Uri mcpRequestUri)
        => string.Equals(
            mcpRequestUri.GetLeftPart(UriPartial.Authority),
            _catalogueServerAuthority,
            StringComparison.OrdinalIgnoreCase);

    private bool UsesATransportThatProtectsTheToken(Uri mcpRequestUri)
        => mcpRequestUri.Scheme == Uri.UriSchemeHttps || _plainTextToTheLoopbackIsPermitted;
}
