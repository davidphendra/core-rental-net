using System.Diagnostics;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.E2E;

/// <summary>
/// The local identity provider is for the browser suite and nothing else, and it says so by
/// refusing to boot rather than by a convention.
/// </summary>
public sealed class LocalProviderGuardTests
{
    [Fact] // AUTH-18
    public async Task The_local_provider_refuses_to_start_outside_development()
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add(TestPaths.LocalProviderAssembly());
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        startInfo.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        startInfo.Environment["Logging__LogLevel__Default"] = "Warning";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The local provider process could not be started.");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        await process.WaitForExitAsync(timeout.Token);

        process.ExitCode.Should().NotBe(0, "production must refuse the provider rather than serve it");
    }
}
