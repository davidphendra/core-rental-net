using CoreRentalNet.Agents.Shared.Scoping;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Ends a run: the connection first, the token second.</summary>
/// <remarks>
/// <b>The order is the whole point.</b> The MCP transport's session-termination request is authenticated with the
/// caller's token, so releasing the token first makes the transport's own cleanup fail — the failure the log
/// reports as "{Endpoint} shutdown failed." The token is given up only once nothing that presents it is open.
/// </remarks>
internal sealed class RunEnding(IRunScopedServiceResolver runScopedServiceResolver) : IRunEnding
{
    public async ValueTask EndTheRunAsync()
    {
        await runScopedServiceResolver.Resolve<IMcpAuthorizationConnection>().CloseAsync();

        // Compatibility path for the legacy token holder: the current host keeps the token in the run context
        // and lets the call scope dispose it, so this is a no-op there.
        if (runScopedServiceResolver.TryResolve<IMcpAccessTokenService>(out var tokens))
        {
            tokens.Release();
        }
    }
}
