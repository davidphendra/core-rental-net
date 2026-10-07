using CoreRentalNet.Agents.Shared.Scoping;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Ends a run: the connection first, the token second.</summary>
/// <remarks>
/// <b>The order is the whole point.</b> The MCP transport's session-termination request is authenticated with the
/// caller's token, so releasing the token first makes the transport's own cleanup fail — the failure the log
/// reports as "{Endpoint} shutdown failed." The token is given up only once nothing that presents it is open.
/// </remarks>
internal sealed class RunEnding(IRunScope runScope) : IRunEnding
{
    public async ValueTask EndTheRunAsync()
    {
        await runScope.Resolve<IMcpAuthorizationConnection>().CloseAsync();

        runScope.Resolve<IMcpAccessTokenService>().Release();
    }
}
