namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>Where the agent is, and whether it may be used at all.</summary>
/// <remarks>
/// <para>
/// Built by the composition root from configuration; <see cref="IsConfigured"/> is false when the feature is
/// off or the endpoint is missing, and the suggestion panel is then <b>absent</b> rather than open - the
/// failure mode of a missing setting is "the feature is hidden", not "the feature is free".
/// </para>
/// <para>
/// <b>How long a run may take is not here, because it is not the application's to decide.</b> A run ends when
/// the agent answers, when it fails, or when the customer stops it.
/// </para>
/// </remarks>
public sealed record MicrosoftFoundryAgentConnectionSettings(
    bool Enabled,
    string ProjectEndpoint,
    string AgentName)
{
    public bool IsConfigured
        => Enabled
            && !string.IsNullOrWhiteSpace(ProjectEndpoint)
            && !string.IsNullOrWhiteSpace(AgentName);

    /// <summary>True when the endpoint is a local stand-in rather than a Foundry project.</summary>
    /// <remarks>
    /// Decided by the scheme, because that is the difference that matters: a Foundry project is reached over
    /// https with the application's identity, and a stand-in is reached over http with none. It is also the
    /// guard the client applies on its own, measured rather than assumed: asked to send a bearer to a
    /// plain-http endpoint, the project client refuses outright.
    /// </remarks>
    public bool IsLocal()
        => Uri.TryCreate(ProjectEndpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == Uri.UriSchemeHttp;
}
