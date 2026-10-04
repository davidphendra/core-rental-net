using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Mcp;

namespace CoreRentalNet.Agents.Shared.Guardrails.Arguments;

/// <summary>Refuses a tool call when this run holds no caller entitled to make it.</summary>
/// <remarks>
/// <para>
/// Scoped like the run, because the fact it reads — the caller's token — is the run's. The guard is deliberately
/// thin: the agent does not validate the token's claims, it only refuses to search when there is no caller at
/// all. Issuer, audience and permission are the catalogue's to enforce, and it does, for every client.
/// </para>
/// <para>
/// It is separate from the allow-list on purpose: the allow-list says which tools exist for this deployment;
/// this says whether this run's caller was entitled to reach one.
/// </para>
/// </remarks>
internal sealed class ToolAuthorizationGuard(IMcpAccessTokenService accessTokens) : IToolGuard
{
    public ValueTask<GuardrailDecision> BeforeAsync(ToolInvocation invocation, CancellationToken cancellationToken)
        => ValueTask.FromResult(
            string.IsNullOrEmpty(accessTokens.Token)
                ? GuardrailDecision.Deny(
                    "The call was refused: this run carries no caller token, so no tool was entitled to it.")
                : GuardrailDecision.Allow());
}
