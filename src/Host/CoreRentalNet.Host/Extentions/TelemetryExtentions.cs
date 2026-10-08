using Azure.Monitor.OpenTelemetry.AspNetCore;
using CoreRentalNet.BuildingBlocks.Application.Telemetry;
using CoreRentalNet.Host.Presentation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace CoreRentalNet.Host.Extentions;

/// <summary>Sends this deployment's telemetry to Application Insights, and names the application.</summary>
/// <remarks>
/// <para>
/// <b>The build identity is a resource attribute, not something an instrument names.</b> A resource is attached
/// once and travels on every trace, metric and log this provider produces, so a dashboard can slice by the
/// release, the commit, the pipeline run or the environment without a line of code in the catalogue, the
/// workspace or the rentals path.
/// </para>
/// <para>
/// <b>The service name is set rather than left to the entry assembly</b>, because one Application Insights
/// resource may well receive the agents as well as this application, and <c>cloud_RoleName</c> is how the two
/// are told apart.
/// </para>
/// <para>
/// <b>A deployment that has not been told where telemetry goes exports none, silently</b> - the rule every other
/// absent setting in this application follows. The meter and the resource are registered either way, so the
/// instruments and their attributes are identical whether or not an exporter is attached.
/// </para>
/// <para>
/// <b>The histogram boundaries are stated rather than defaulted.</b> The defaults stop at 10,000, so every
/// rupiah order value would land in the top bucket and the percentiles would say nothing at all.
/// </para>
/// </remarks>
internal static class TelemetryExtentions
{
    /// <summary>The connection string Application Insights is reached with. Never a value in this repository.</summary>
    private const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>What this application is called in every backend it reports to.</summary>
    private const string ServiceName = "core-rental-app";

    /// <summary>Rupiah amounts, from one month to a year paid up front.</summary>
    private static readonly double[] RupiahBoundaries =
        [0, 100_000, 250_000, 500_000, 1_000_000, 2_500_000, 5_000_000, 10_000_000];

    public static void AddTelemetry(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var build = AppBuildIdentity.Current;
        var connectionString = builder.Configuration[ConnectionStringKey];

        var telemetry = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddMeter(BusinessTelemetry.Name)
                .AddView("suggestion.run.duration", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = [0, 5, 10, 20, 30, 45, 60, 90, 120, 300],
                })
                .AddView("order.value.first_invoice", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = RupiahBoundaries,
                })
                .AddView("order.value.monthly", new ExplicitBucketHistogramConfiguration
                {
                    Boundaries = RupiahBoundaries,
                }))
            .ConfigureResource(resource => resource
                .AddService(ServiceName)
                .AddAttributes(build.ResourceAttributes()));

        if (!string.IsNullOrWhiteSpace(builder.Configuration[ConnectionStringKey]))
        {
            telemetry.UseAzureMonitor();
            telemetry.UseAzureMonitor(options => options.ConnectionString = connectionString);
        }
    }
}
