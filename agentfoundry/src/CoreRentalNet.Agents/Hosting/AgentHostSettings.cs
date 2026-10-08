using CoreRentalNet.Agents.Features.EchoReverse;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion;
using CoreRentalNet.Agents.Shared.Diagnostics;
using CoreRentalNet.Agents.Shared.Model;

namespace CoreRentalNet.Agents.Hosting;

/// <summary>Everything the host reads from configuration, resolved once and carried as one value.</summary>
/// <remarks>
/// The composition root takes this instead of reading keys itself, so a key has one reader and the startup
/// report and the registrations cannot disagree about what a setting said. Each member is resolved by the type
/// that owns it — an identity by its own <c>FromConfiguration</c>, the transport by its options type — so no
/// parsing rule is restated here.
/// </remarks>
internal sealed record AgentHostSettings
{
    /// <summary>The key naming which agent a request that carries no name should get.</summary>
    public const string DefaultAgentNameKey = "AgentHost:DefaultAgentName";

    /// <summary>The build identity stamped into the artifact, used by telemetry and by the startup report.</summary>
    public required AgentBuildIdentity Build { get; init; }

    /// <summary>The name the workspace-suggestion agent is registered and resolved under.</summary>
    public required WorkspaceSuggestionAgentIdentity WorkspaceSuggestion { get; init; }

    /// <summary>The agent a request that names none gets: the deployment's own, or the workspace agent.</summary>
    public required string DefaultAgentName { get; init; }

    /// <summary>Whether the echo agent is served, and the name it is served under.</summary>
    public required EchoAgentIdentity Echo { get; init; }

    /// <summary>The model transport's budget, which is not a workflow's attempt count.</summary>
    public required ModelTransportRetryOptions ModelTransport { get; init; }

    /// <summary>The Foundry project endpoint, or null when nobody set it. Printed as presence, never as a value.</summary>
    public string? ProjectEndpoint { get; init; }

    /// <summary>The model deployment every stage runs on, or null when nobody set it.</summary>
    public string? ModelDeployment { get; init; }

    /// <summary>The settings a deployment's configuration describes.</summary>
    public static AgentHostSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var workspaceSuggestion = WorkspaceSuggestionAgentIdentity.FromConfiguration(configuration);
        var echo = EchoAgentIdentity.FromConfiguration(configuration);

        // Which agent a request that names none should get. Two azd behaviours meet here and neither carries the
        // answer: `azd ai agent run <service>` does not pass the service name through locally, and
        // `azd ai agent invoke --local` sends a request with no agent name at all. A deployment that serves more
        // than one agent therefore says which one it is, per service, in azure.yaml.
        var defaultAgentName = configuration[DefaultAgentNameKey]?.Trim() is { Length: > 0 } namedAgent
            ? namedAgent
            : workspaceSuggestion.AgentName;

        // A default that names an agent this deployable switches off resolves nothing at request time, and the
        // failure would surface only then, as a hosting error about a missing default. Refuse it at startup,
        // by name, while the setting can still be corrected.
        if (string.Equals(defaultAgentName, echo.AgentName, StringComparison.Ordinal) && !echo.IsEnabled)
        {
            throw new InvalidOperationException(
                $"{DefaultAgentNameKey} names '{defaultAgentName}', which {EchoAgentIdentity.IsEnabledKey} "
                + "turns off, so a request that carries no name would resolve no agent.");
        }

        return new AgentHostSettings
        {
            Build = AgentBuildIdentity.Current,
            WorkspaceSuggestion = workspaceSuggestion,
            DefaultAgentName = defaultAgentName,
            Echo = echo,
            ModelTransport = configuration.GetSection("ModelTransport").Get<ModelTransportRetryOptions>()
                ?? new ModelTransportRetryOptions(),
            ProjectEndpoint = configuration[AgentFoundryRegistration.ProjectEndpointKey]?.Trim(),
            ModelDeployment = configuration[AgentFoundryRegistration.ModelKey]?.Trim(),
        };
    }
}
