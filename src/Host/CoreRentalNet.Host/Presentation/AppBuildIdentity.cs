namespace CoreRentalNet.Host.Presentation;

/// <summary>The build a deployment is correlated by: the release, the commit, the pipeline run and where it runs.</summary>
/// <remarks>
/// <para>
/// Every value is stamped into the artifact by the pipeline, so the identity describes the build and cannot be
/// changed by whoever starts the process. It is contributed as OpenTelemetry resource attributes, which is what
/// makes every trace, metric and log carry it without a single instrument naming it, and it is also what the
/// footer reads. The keys are the deployment's own rather than <c>service.*</c>, which the platform fills.
/// </para>
/// <para>
/// The environment is not the exception: it is the deployment the artifact was built for, not a property of the
/// machine it happens to run on, so it is baked in with the rest rather than read from the host at run time. A
/// build that named no environment reports the local default.
/// </para>
/// </remarks>
internal sealed record AppBuildIdentity
{
    /// <summary>What a build that named no environment reports.</summary>
    public const string LocalEnvironment = "local";

    /// <summary>The release, without build metadata.</summary>
    public required string Version { get; init; }

    /// <summary>The commit the artifact was built from, or null when the build carried none.</summary>
    public string? GitSha { get; init; }

    /// <summary>The pipeline run that produced the artifact, or null when the build carried none.</summary>
    public string? BuildId { get; init; }

    /// <summary>Where the artifact was built to run: a deployment's name, or <see cref="LocalEnvironment"/>.</summary>
    public required string Environment { get; init; }

    /// <summary>The running process's identity, read from the artifact and nothing else.</summary>
    public static AppBuildIdentity Current => Create(
        AppVersion.Current,
        AppVersion.GitSha,
        AppVersion.BuildId,
        AppVersion.Environment);

    /// <summary>An identity from its parts, defaulting the environment when the build named none.</summary>
    public static AppBuildIdentity Create(string version, string? gitSha, string? buildId, string? environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        return new AppBuildIdentity
        {
            Version = version,
            GitSha = Blank(gitSha),
            BuildId = Blank(buildId),
            Environment = Blank(environment) ?? LocalEnvironment,
        };
    }

    /// <summary>What the footer shows: the fields the build carried, on one line.</summary>
    public string Label => string.Join(" · ", Parts());

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

    /// <summary>The fields the footer shows, in the order a reader wants them: what, where, then which build.</summary>
    private IEnumerable<string> Parts()
    {
        yield return Version;
        yield return Environment;

        if (GitSha is { } gitSha)
        {
            yield return gitSha;
        }

        if (BuildId is { } buildId)
        {
            yield return buildId;
        }
    }

    private static string? Blank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
