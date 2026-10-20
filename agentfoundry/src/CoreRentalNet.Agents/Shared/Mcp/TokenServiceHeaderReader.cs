namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Adapts the legacy test token holder to the header-reader seam.</summary>
internal sealed class TokenServiceHeaderReader(IMcpAccessTokenService tokens) : IAccessTokenHeaderReader
{
    public string? ReadCallerAccessToken() => tokens.Token;
}
