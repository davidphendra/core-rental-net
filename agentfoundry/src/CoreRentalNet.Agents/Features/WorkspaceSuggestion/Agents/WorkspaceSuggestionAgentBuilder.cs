using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Scoping;
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
/// <b>The stage agents belong to the graph, and the graph outlives the request.</b> Every decorator here that needs
/// the run's token service, its connection or its model-call telemetry is handed the run scope instead, and reaches
/// the run's own instance at the call. That is what lets the served agent be a singleton.
/// </para>
/// </remarks>
internal sealed class WorkspaceSuggestionAgentBuilder(
    IRunScope runScope,
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
            _captureContent
        );

    private IChatClient StageChatClient(AgentProfile agentProfile)
        => new AuthorisedMcpChatClient(
            RegularChatClient(agentProfile),
            runScope,
            agentProfile.UsesCatalogueTools,
            toolAllowList,
            loggerFactory.CreateLogger<AuthorisedMcpChatClient>()
        );

    private IChatClient RegularChatClient(AgentProfile agentProfile)
        => new FunctionInvokingChatClient(new RunScopedChatClient(runScope), loggerFactory)
            .AsBuilder()
            .UseOpenTelemetry(
                loggerFactory,
                WorkspaceTelemetry.Name,
                configure: client => client.EnableSensitiveData = _captureContent)
            .Build();
}
