namespace CoreRentalNet.Agents.Shared.Diagnostics;

/// <summary>The build identity a deployment is correlated by: the release, the commit, the pipeline run and where it runs.</summary>
/// <remarks>
/// <para>
/// Every value is stamped into the artifact by the pipeline, so the identity describes the build and cannot
/// be changed by whoever starts the container. The environment is not the exception: it is the deployment the
/// artifact was built for, not a property of the machine it happens to run on, and it is baked in with the
/// rest rather than injected at run time. A build that named no environment reports the local default.
/// </para>
/// <para>
/// It is contributed as OpenTelemetry resource attributes. The keys are the deployment's own rather than
/// <c>service.*</c>, which the platform already fills.
/// </para>
/// </remarks>
internal sealed record AgentBuildIdentity
{
    /// <summary>What a build that named no environment reports.</summary>
    public const string LocalEnvironment = "local";

    /// <summary>The release, without build metadata.</summary>
    public required string Version { get; init; }

    /// <summary>The commit the artifact was built from, or null when the build carried none.</summary>
    public string? GitSha { get; init; }

    /// <summary>The pipeline run that produced the artifact, or null when the build carried none.</summary>
    public string? BuildId { get; init; }

    /// <summary>Where the artifact runs: a deployment's name, or <see cref="LocalEnvironment"/>.</summary>
    public required string Environment { get; init; }

    /// <summary>The running process's identity, read from the artifact and nothing else.</summary>
    public static AgentBuildIdentity Current => Create(
        AgentVersion.Current,
        AgentVersion.GitSha,
        AgentVersion.BuildId,
        AgentVersion.Environment);

    /// <summary>An identity from its parts, defaulting the environment when the build named none.</summary>
    public static AgentBuildIdentity Create(string version, string? gitSha, string? buildId, string? environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        return new AgentBuildIdentity
        {
            Version = version,
            GitSha = Blank(gitSha),
            BuildId = Blank(buildId),
            Environment = Blank(environment) ?? LocalEnvironment,
        };
    }

    /// <summary>The resource attributes this identity contributes, omitting what the build did not carry.</summary>
    public IEnumerable<KeyValuePair<string, object>> ResourceAttributes()
    {
        yield return new("version", Version);
        yield return new("environment", Environment);

        if (GitSha is { } gitSha)
        {
            yield return new("gitSha", gitSha);
        }

        if (BuildId is { } buildId)
        {
            yield return new("buildId", buildId);
        }
    }

    private static string? Blank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
