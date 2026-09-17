using Microsoft.Extensions.Configuration;

namespace CoreRentalNet.Host.Agents;

/// <summary>Where the hosted agent is, and what it is called.</summary>
/// <remarks>
/// Both from configuration, because the endpoint differs per environment and the agent's name is the
/// platform's registration rather than something this application may invent. Absent means no agent is
/// configured, which is the state the application starts in and which the page reports as unavailable
/// rather than as a refusal.
/// </remarks>
internal sealed record FoundryAgentSettings(string? Endpoint, string? AgentName, bool UseManagedIdentity)
{
    /// <summary>The scope a token for the agent endpoint is requested for.</summary>
    public const string Scope = "https://ai.azure.com/.default";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint);

    /// <summary>True when the endpoint is a local stand-in rather than a Foundry project.</summary>
    /// <remarks>
    /// Decided by the scheme, because that is the difference that matters: a Foundry project is reached
    /// over https with the application's identity, and a stand-in is reached over http with none. It is
    /// also the guard the Foundry client applies on its own - it will not send a bearer to plain http -
    /// so this is where the two paths part.
    /// </remarks>
    public bool IsLocal()
        => Endpoint is not null
            && Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == Uri.UriSchemeHttp;

    /// <summary>The per-agent endpoint the platform routes to the container's own Responses route.</summary>
    /// <remarks>
    /// Derived rather than configured, because it is the platform's shape and not a deployment choice:
    /// the endpoint names the project and the agent, and the path is the platform's.
    /// </remarks>
    public string AgentEndpoint()
    {
        ArgumentNullException.ThrowIfNull(Endpoint);

        return string.IsNullOrWhiteSpace(AgentName)
            ? Endpoint
            : $"{Endpoint}/agents/{AgentName}/endpoint/protocols/openai";
    }

    public static FoundryAgentSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new FoundryAgentSettings(
            configuration["Agent:Endpoint"]?.Trim().TrimEnd('/'),
            configuration["Agent:Name"]?.Trim(),
            configuration.GetValue("Agent:UseManagedIdentity", defaultValue: false));
    }
}
