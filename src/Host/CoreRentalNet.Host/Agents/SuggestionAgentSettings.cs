namespace CoreRentalNet.Host.Agents;

/// <summary>Where the agent is, whether it may be used at all, and how long a run may take.</summary>
/// <remarks>
/// Everything comes from configuration. <see cref="IsConfigured"/> is false when the feature is off or the
/// endpoint is missing, and the AI section is then <b>absent</b> rather than open — the failure mode of a
/// missing setting is "the feature is hidden", not "the feature is free".
/// </remarks>
internal sealed record SuggestionAgentSettings(
    bool Enabled,
    string ProjectEndpoint,
    string AgentName,
    int TimeoutSeconds)
{
    /// <summary>Above the p95 target, so a slow-but-correct run finishes rather than being cut off.</summary>
    public const int DefaultTimeoutSeconds = 45;

    public bool IsConfigured
        => Enabled
            && !string.IsNullOrWhiteSpace(ProjectEndpoint)
            && !string.IsNullOrWhiteSpace(AgentName);

    /// <summary>True when the endpoint is a local stand-in rather than a Foundry project.</summary>
    /// <remarks>
    /// <para>
    /// Decided by the scheme, because that is the difference that matters: a Foundry project is reached over
    /// https with the application's identity, and a stand-in is reached over http with none.
    /// </para>
    /// <para>
    /// It is also the guard the client applies on its own, and that was <b>measured rather than assumed</b>:
    /// asked to send a bearer to a plain-http endpoint, the project client refuses with
    /// <c>InvalidOperationException: Bearer token authentication is not permitted for non TLS protected (https)
    /// endpoints</c>. So there is no credential that can be presented to a stand-in, and the two paths really
    /// do part here rather than merely differing in configuration.
    /// </para>
    /// </remarks>
    public bool IsLocal()
        => Uri.TryCreate(ProjectEndpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == Uri.UriSchemeHttp;

    public static SuggestionAgentSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new SuggestionAgentSettings(
            configuration.GetValue("Agent:Enabled", false),
            configuration["Agent:ProjectEndpoint"]?.Trim() ?? string.Empty,
            configuration["Agent:AgentName"]?.Trim() ?? "core-rental-workspace-suggestion-agent",
            configuration.GetValue("Agent:TimeoutSeconds", DefaultTimeoutSeconds));
    }
}
