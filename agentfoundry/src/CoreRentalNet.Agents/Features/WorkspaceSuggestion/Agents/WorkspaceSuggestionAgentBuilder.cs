using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Telemetry;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

/// <summary>Builds the call's stage agents, each with the cross-cutting concerns composed in.</summary>
/// <remarks>
/// <para>
/// <b>This is the one place the per-stage decorator order is written down.</b> Outside in: the mcpAuthorizationConnection
/// decorator offers the tools this call's token entitles; the function loop resolves the tools the model calls; and
/// the run-scoped telemetry is last. The order matters - a decorator below the function loop cannot be reached for a
/// tool-calling turn - so it is stated here. The telemetry's own inner chain - the token-leak guardrail and then the
/// model client - is composed where that one instance is registered, because it records the whole run and not one
/// stage of it.
/// </para>
/// <para>
/// Scoped like the run: every decorator here holds the run's token service, its mcpAuthorizationConnection connection and its
/// model-call telemetry.
/// </para>
/// </remarks>
internal sealed class WorkspaceSuggestionAgentBuilder(
    IMcpAccessTokenService accessTokenService,
    IMcpAuthorizationConnection mcpAuthorizationConnection,
    ITelemetryChatClient telemetryChatClient,
    IGuardrailFunctionMiddleware guardrailMiddleware,
    IToolAllowList toolAllowList,
    IConfiguration configuration,
    ILoggerFactory loggerFactory)
{
    private readonly bool _captureContent = configuration.GetValue(WorkspaceTelemetry.CaptureContentConfigurationKey, true);

    /// <summary>The agent for one roster entry, built the same way every stage is.</summary>
    public AIAgent For(AgentProfile agentProfile)
        => AgentFactory.Build(
            agentProfile,
            agentProfile.UsesCatalogueTools
                ? StageChatClient(agentProfile)
                : RegularChatClient(agentProfile),
            _captureContent,
            guardrailMiddleware
        );

    private IChatClient StageChatClient(AgentProfile agentProfile)
        => new AuthorisedMcpChatClient(
            RegularChatClient(agentProfile),
            accessTokenService,
            mcpAuthorizationConnection,
            agentProfile.UsesCatalogueTools,
            toolAllowList,
            loggerFactory.CreateLogger<AuthorisedMcpChatClient>()
        );

    private IChatClient RegularChatClient(AgentProfile agentProfile)
        => new FunctionInvokingChatClient(telemetryChatClient, loggerFactory)
            .AsBuilder()
            .UseOpenTelemetry(
                loggerFactory,
                WorkspaceTelemetry.Name,
                configure: client => client.EnableSensitiveData = _captureContent)
            .Build();
}
