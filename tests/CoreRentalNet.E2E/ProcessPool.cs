using System.Diagnostics;

namespace CoreRentalNet.E2E;

/// <summary>
/// The processes the suite has started, kept together so they can be waited for and stopped once.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here is a test double. The suite runs the published hosts exactly as a person would,
/// because the requirement is full user interaction with no mocks and no intercepted calls: a
/// stubbed server would make the suite a test of the stubs (ADR-0020).
/// </para>
/// <para>
/// Every process is tracked from the moment it starts, so a failure part-way through starting the
/// hosts still stops everything that was already running.
/// </para>
/// </remarks>
internal sealed class ProcessPool : IAsyncDisposable
{
    private readonly List<AppProcess> processes = [];

    public AppProcess Start(string assembly, string workingDirectory, Dictionary<string, string> environment)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add(assembly);

        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"The process for {assembly} could not be started.");

        var tracked = new AppProcess(process);
        processes.Add(tracked);

        // Drained so the process never blocks on a full pipe, and kept so a start-up failure can
        // report what the process actually said.
        process.OutputDataReceived += (_, args) => tracked.Record(args.Data);
        process.ErrorDataReceived += (_, args) => tracked.Record(args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return tracked;
    }

    /// <summary>
    /// Waits until the application answers, and - when asked - until its stylesheet does too.
    /// </summary>
    /// <remarks>
    /// The root page answering is not enough. A build started from the wrong directory serves it
    /// happily while every asset behind it is missing, so the stylesheet is fetched as the proof that
    /// the application found its own files.
    /// </remarks>
    public static async Task WaitUntilReadyAsync(string baseUrl, AppProcess process, bool requireAssets)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"The application at {baseUrl} exited with code {process.ExitCode} before becoming ready." +
                    $"{Environment.NewLine}{process.Output()}");
            }

            try
            {
                var response = await client.GetAsync(new Uri(baseUrl)).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    if (!requireAssets)
                    {
                        return;
                    }

                    var asset = await client.GetAsync(new Uri($"{baseUrl}/styles/tokens.css")).ConfigureAwait(false);

                    if (asset.IsSuccessStatusCode)
                    {
                        return;
                    }

                    throw new InvalidOperationException(
                        $"The application at {baseUrl} is answering but /styles/tokens.css returned {asset.StatusCode}. " +
                        $"It is running from {TestPaths.ProjectDirectory()} but its static assets are somewhere else.");
                }
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
            catch (TaskCanceledException)
            {
                // Still starting.
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"The application at {baseUrl} did not become ready within 90 seconds.{Environment.NewLine}{process.Output()}");
    }

    /// <summary>Waits until the local provider publishes its discovery document.</summary>
    public static async Task WaitUntilProviderReadyAsync(string authority, AppProcess process)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
        var discovery = $"{authority}/.well-known/openid-configuration";

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException(
                    $"The local identity provider exited with code {process.ExitCode} before becoming ready." +
                    $"{Environment.NewLine}{process.Output()}");
            }

            try
            {
                var response = await client.GetAsync(new Uri(discovery)).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
            catch (TaskCanceledException)
            {
                // Still starting.
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"The local identity provider at {authority} did not become ready within 90 seconds." +
            $"{Environment.NewLine}{process.Output()}");
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var process in processes)
        {
            await process.StopAsync().ConfigureAwait(false);
        }
    }
}
