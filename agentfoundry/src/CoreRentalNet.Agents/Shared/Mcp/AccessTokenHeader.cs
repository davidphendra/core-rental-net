namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>The header the application states the caller's catalogue token on.</summary>
/// <remarks>
/// <b>Stated here and again in the application, and it cannot be stated once.</b> The two are separate deployables
/// with no shared assembly — the same way the catalogue's tool limits are stated at both ends. The
/// <c>x-client-</c> prefix is the platform's forwarding rule rather than decoration: the hosting layer's request
/// context exposes the client headers, "those prefixed with <c>x-client-</c>", so a header outside that prefix is
/// not forwarded at all — and a dropped token is indistinguishable from a catalogue refusal.
/// </remarks>
internal static class AccessTokenHeader
{
    public const string Name = "x-client-mcp-catalog-access-token";
}
