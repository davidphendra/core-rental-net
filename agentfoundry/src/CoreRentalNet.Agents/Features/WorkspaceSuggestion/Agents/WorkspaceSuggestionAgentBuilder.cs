using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Shared.ChatClients;
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
/// <b>The stage agents belong to one request's graph.</b> Every decorator here that needs the run's token service,
/// its connection or its model-call telemetry is handed the run scope instead, and reaches the run's own instance
/// at the call. The builder is a stateless singleton; the agents it creates are not shared between requests.
/// </para>
/// </remarks>
internal sealed class WorkspaceSuggestionAgentBuilder(
    IRunScopedServiceResolver runScopedServiceResolver,
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
            CatalogueToolChatClient(agentProfile),
            runScopedServiceResolver,
            loggerFactory.CreateLogger<AuthorisedMcpChatClient>()
        );

    /// <summary>The tool-backed client for the one stage that searches the catalogue.</summary>
    /// <remarks>
    /// This chain is separate from <see cref="RegularChatClient"/> because its shape is different: the
    /// tool-then-JSON decorator is only meaningful when a function loop can follow a tool call with a
    /// structured answer. A stage that is offered no catalogue tools must not carry that decorator.
    /// </remarks>
    private IChatClient CatalogueToolChatClient(AgentProfile agentProfile)
        => new FunctionInvokingChatClient(
            new ToolThenJsonChatClient(
                new RunScopedChatClient(runScopedServiceResolver),
                agentProfile.Output,
                loggerFactory.CreateLogger<ToolThenJsonChatClient>()),
            loggerFactory)
            .AsBuilder()
            .UseOpenTelemetry(
                loggerFactory,
                WorkspaceTelemetry.Name,
                configure: client => client.EnableSensitiveData = _captureContent)
            .Build();

    private IChatClient RegularChatClient(AgentProfile agentProfile)
        => new FunctionInvokingChatClient(new RunScopedChatClient(runScopedServiceResolver), loggerFactory)
            .AsBuilder()
            .UseOpenTelemetry(
                loggerFactory,
                WorkspaceTelemetry.Name,
                configure: client => client.EnableSensitiveData = _captureContent)
            .Build();
}
