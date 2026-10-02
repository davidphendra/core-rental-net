using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Model;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages;
using Xunit;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Requests;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The composition this host is built from. It was the least-covered file in the agent project, and the
/// parts that were uncovered are exactly the parts a deployment depends on.
/// </summary>
public sealed class AgentFoundryRegistrationTests
{
    /// <summary>The name the test configuration carries, standing in for the deployed agent name.</summary>
    private const string SuggestionAgentName = "core-rental-workspace-suggestion-agent";

    [Fact]
    public void A_missing_project_endpoint_is_refused_by_name()
    {
        var act = () => AgentFoundryRegistration.BuildChatClient(Configuration(
            ("AZURE_AI_MODEL_DEPLOYMENT_NAME", "gpt-4.1-mini")));

        act.Should().Throw<InvalidOperationException>().WithMessage("*FOUNDRY_PROJECT_ENDPOINT*");
    }

    [Fact]
    public void A_missing_model_deployment_is_refused_by_name()
    {
        var act = () => AgentFoundryRegistration.BuildChatClient(Configuration(
            ("FOUNDRY_PROJECT_ENDPOINT", "https://example.invalid/api/projects/p")));

        act.Should().Throw<InvalidOperationException>().WithMessage("*AZURE_AI_MODEL_DEPLOYMENT_NAME*");
    }

    [Fact]
    public void An_empty_setting_counts_as_missing_rather_than_as_a_value()
    {
        // An environment variable set to nothing is the shape a half-finished deployment actually has.
        var act = () => AgentFoundryRegistration.BuildChatClient(Configuration(
            ("FOUNDRY_PROJECT_ENDPOINT", "  "),
            ("AZURE_AI_MODEL_DEPLOYMENT_NAME", "gpt-4.1-mini")));

        act.Should().Throw<InvalidOperationException>().WithMessage("*FOUNDRY_PROJECT_ENDPOINT*");
    }

    [Fact]
    public void The_model_transport_budget_is_configuration_rather_than_a_constant()
    {
        var transportRetryOptions = Configuration(
                ("ModelTransport:NetworkTimeout", "00:05:00"),
                ("ModelTransport:MaximumRetryAttempts", "4"))
            .GetSection("ModelTransport")
            .Get<ModelTransportRetryOptions>();

        transportRetryOptions!.NetworkTimeout.Should().Be(TimeSpan.FromMinutes(5));
        transportRetryOptions.MaximumRetryAttempts.Should().Be(4);
    }

    [Fact]
    public void The_workflow_is_published_as_the_single_named_agent_foundry_resolves()
    {
        // The served agent has to be the workflow agent itself — not a wrapper — or hosting cannot redirect
        // this workflow's checkpoints and the readiness probe fails. A wrapped agent would still carry this
        // name, so the name alone does not prove it; the hosted readiness test is what would.
        var agent = Workflow().AsAIAgent();

        agent.Name.Should().Be(SuggestionAgentName);
    }

    [Fact]
    public void The_run_telemetry_is_resolved_by_its_port()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IMcpAccessTokenService>(
            _ => new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance));

        services.AddWorkspaceSuggestionFeature(
            Configuration((WorkspaceSuggestionAgentIdentity.ConfigurationKey, SuggestionAgentName)),
            new ScriptedChatClient("{}"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITelemetryChatClient>()
            .Should().BeOfType<TelemetryChatClient>(
                "callers depend on the port and DI injects the sealed implementation");
    }

    private static WorkspaceSuggestionWorkflow Workflow()
    {
        var tokens = new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance);
        var catalog = new McpSetting(string.Empty);
        var recordedToolAnswers = new McpToolAnswerLedger();
        var modelCallTelemetry = new TelemetryChatClient(
            new GuardrailChatClient(new ScriptedChatClient("{}"), tokens),
            "gpt-4.1-mini",
            "test-prompts",
            NullLogger<TelemetryChatClient>.Instance);

        var stageAgents = new WorkspaceSuggestionAgentBuilder(
            tokens,
            new McpAuthorizationConnection(
                catalog,
                tokens,
                NullLoggerFactory.Instance,
                NullLogger<McpAuthorizationConnection>.Instance),
            recordedToolAnswers,
            modelCallTelemetry,
            NullLoggerFactory.Instance);

        var executorBuilder = new WorkspaceSuggestionExecutorBuilder(
            stageAgents,
            tokens,
            TheInvocationARunArrivesIn.CarryingNothing(),
            new WorkspaceSuggestionWorkflowOptions(),
            recordedToolAnswers,
            modelCallTelemetry,
            NullLoggerFactory.Instance);

        return new(
            executorBuilder,
            new WorkspaceSuggestionAgentIdentity { AgentName = SuggestionAgentName });
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value))
            .Build();
}
