using CoreRentalNet.Host.Presentation;

namespace CoreRentalNet.Host.Extentions;

/// <summary>Prints which build this deployment is, before anything else can fail.</summary>
/// <remarks>
/// <para>
/// The same four values the telemetry resource carries and the footer shows, on the log stream rather than in
/// the browser: an operator reading a container's output has to be able to say which artifact is running
/// without asking a customer to scroll to the foot of a page. It runs first, so a deployment that never becomes
/// ready still says what it was.
/// </para>
/// <para>
/// <b>A field the build did not carry prints <c>(unset)</c> rather than being omitted</b>, which is the
/// opposite of the footer and deliberately so: a customer should not be shown an empty field, and an operator
/// has to be able to see which field the pipeline failed to stamp.
/// </para>
/// </remarks>
internal static class StartupDiagnosticExtentions
{
    private const string Category = "CoreRentalNet.Host.Composition.StartupDiagnosticExtentions";

    /// <summary>What this application calls itself on the log. The same name the telemetry resource sets.</summary>
    private const string ServiceName = "core-rental-app";

    /// <summary>What a field the build did not carry reads as.</summary>
    private const string Unset = "(unset)";

    public static void AnnounceBuildIdentity(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        Announce(
            app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(Category),
            AppBuildIdentity.Current);
    }

    /// <summary>The announcement itself, over a logger, so the rule is tested without a running host.</summary>
    internal static void Announce(ILogger logger, AppBuildIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(identity);

        logger.LogInformation("[startup] {ServiceName}", ServiceName);
        logger.LogInformation("[startup]   version     : {Version}", identity.Version);
        logger.LogInformation("[startup]   environment : {Environment}", identity.Environment);
        logger.LogInformation("[startup]   git sha     : {GitSha}", identity.GitSha ?? Unset);
        logger.LogInformation("[startup]   build id    : {BuildId}", identity.BuildId ?? Unset);
    }
}
