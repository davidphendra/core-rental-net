using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// A deployment that has been told where telemetry goes starts.
/// </summary>
/// <remarks>
/// The failure this proves gone: the exporter was attached by one <c>UseAzureMonitor</c> call and then
/// configured by a second, and the SDK refuses the second registration outright -
/// <c>Multiple calls to UseAzureMonitorExporter on the same IServiceCollection are not supported</c> -
/// while the host is starting. It is the worst shape a registration bug can take: the application does
/// not come up at all, and it does so only in a deployment that set the connection string, because a
/// deployment that set nothing never reached either call.
/// </remarks>
public sealed class TelemetryRegistrationTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), $"core-rental-telemetry-{Guid.NewGuid():N}");

    [Fact]
    public void The_host_starts_when_a_telemetry_connection_string_is_configured()
    {
        var database = Path.Combine(_directory, "corerental.db");

        // A key and an endpoint in the shape the SDK parses, so the string is accepted rather than
        // judged - nothing is ever sent to it, because starting the host is the whole subject here.
        using var host = new NonDevelopmentHostFactory(
            database,
            [
                new KeyValuePair<string, string?>(
                    "ApplicationInsights:ConnectionString",
                    "InstrumentationKey=00000000-0000-0000-0000-000000000000;"
                    + "IngestionEndpoint=https://example.invalid/"),
            ]);

        // Building the host is where the duplicate registration threw, so reaching the services at all
        // is the assertion; the file exists so this test says why rather than passing by never asking.
        host.Services.Should().NotBeNull(
            "the host came up with an exporter attached, rather than failing to start");
    }

    public void Dispose()
    {
        if (!Directory.Exists(_directory))
        {
            return;
        }

        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort: a leftover temp directory is not worth failing a run over.
        }
    }
}
