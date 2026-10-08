using CoreRentalNet.Agents.Shared.Diagnostics;
using CoreRentalNet.Agents.Shared.Telemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CoreRentalNet.Agents.Hosting;

/// <summary>Tells the platform's telemetry providers what this application emits and what must be scrubbed.</summary>
/// <remarks>
/// The hosting platform builds the providers and chooses the exporters; this only names the application's own
/// source and meter, its redaction processor, and the build identity every span, metric and log record carries.
/// </remarks>
internal static class HostTelemetry
{
    /// <summary>Adds the application's source, meter, redaction and build identity to the host's providers.</summary>
    public static AgentHostBuilder AddHostTelemetry(this AgentHostBuilder builder, AgentBuildIdentity build)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(build);

        // The source and the meter are named here because the distro cannot know an application's own vocabulary.
        // The build identity rides the resource, so no instrument has to name it; the keys are ours because the
        // platform already fills service.*.
        builder.ConfigureTracing(tracing => tracing
            .AddSource(WorkspaceTelemetry.Name)
            .AddProcessor(new TokenLeakRedactionProcessor())
            .ConfigureResource(resource => resource.AddAttributes(build.ResourceAttributes())));

        builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics
            .AddMeter(WorkspaceTelemetry.Name)
            .ConfigureResource(resource => resource.AddAttributes(build.ResourceAttributes())));

        // The hosting package sets the logger provider's resource after ours, so the identity cannot ride that
        // resource. It is stamped on each log record instead, through a processor the hosting adds from these
        // options.
        builder.Services.Configure<OpenTelemetryLoggerOptions>(logging =>
            logging.AddProcessor(new AgentBuildIdentityLogProcessor(build)));

        return builder;
    }
}
