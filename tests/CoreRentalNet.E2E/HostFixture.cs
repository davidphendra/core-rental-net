using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;
using Xunit;

namespace CoreRentalNet.E2E;

/// <summary>
/// A real application, started as a real process, on a real port, with its own database file.
/// </summary>
/// <remarks>
/// Nothing here is a test double. The suite runs the published host exactly as a person would,
/// because the requirement is full user interaction with no mocks and no intercepted calls: a
/// stubbed server would make the suite a test of the stubs.
/// <para>
/// Started once for the whole collection. Isolation between tests comes from a fresh browser
/// context per test, which gives each test its own draft cookie and therefore its own workspace.
/// </para>
/// </remarks>
public sealed class HostFixture : IAsyncLifetime
{
    private Process? host;
    private string? workingDirectory;

    public string BaseUrl { get; private set; } = string.Empty;

    public IPlaywright Playwright { get; private set; } = null!;

    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var port = FreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        // Where the database goes. A throwaway directory, so a run cannot touch real data.
        workingDirectory = Path.Combine(Path.GetTempPath(), $"core-rental-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        host = StartHost(port);

        await WaitUntilReadyAsync().ConfigureAwait(false);

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true })
            .ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync().ConfigureAwait(false);
        }

        Playwright?.Dispose();

        if (host is { HasExited: false })
        {
            host.Kill(entireProcessTree: true);
            await host.WaitForExitAsync().ConfigureAwait(false);
        }

        host?.Dispose();

        if (workingDirectory is not null && Directory.Exists(workingDirectory))
        {
            try
            {
                Directory.Delete(workingDirectory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort: a leftover temp directory is not worth failing a run over.
            }
        }
    }

    /// <summary>A browser context is a browser profile: fresh cookies, so a fresh workspace.</summary>
    public async Task<IPage> NewPageAsync(ViewportSize? viewport = null)
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = viewport ?? new ViewportSize { Width = 1400, Height = 1000 },
        }).ConfigureAwait(false);

        var page = await context.NewPageAsync().ConfigureAwait(false);
        page.SetDefaultTimeout(20_000);

        return page;
    }

    private readonly Queue<string> hostOutput = new();

    private void Record(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (hostOutput)
        {
            hostOutput.Enqueue(line);

            while (hostOutput.Count > 40)
            {
                hostOutput.Dequeue();
            }
        }
    }

    private string HostOutput()
    {
        lock (hostOutput)
        {
            return hostOutput.Count == 0 ? "(the application said nothing)" : string.Join(Environment.NewLine, hostOutput);
        }
    }

    private Process StartHost(int port)
    {
        var hostDll = HostAssemblyPath();

        var startInfo = new ProcessStartInfo("dotnet")
        {
            // Started from the application's own directory, exactly as dotnet run does. A
            // development build serves its static assets, its fonts and its vendored product
            // images from there, so starting it anywhere else loses all of them silently.
            WorkingDirectory = ProjectDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add(hostDll);

        // The database is a throwaway file in a throwaway directory, so a run cannot contaminate
        // the developer's own data and cannot be contaminated by it.
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        startInfo.Environment["Sqlite__DatabasePath"] = Path.Combine(workingDirectory!, "e2e.db");

        // Identity off, explicitly and by configuration rather than by having no credentials.
        // Development loads appsettings.Local.json, that file is loaded last so it wins over the
        // environment, and on a machine that has real Auth0 credentials the catalog gate would then be
        // live for every test - which is how nine of them failed before this line existed. The suite
        // has to run the application the way it ships, not the way one laptop is configured.
        startInfo.Environment["Auth0__Enabled"] = "false";
        startInfo.Environment["Rentals__SchedulerIntervalMinutes"] = "1";
        startInfo.Environment["Logging__LogLevel__Default"] = "Warning";

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The application process could not be started.");

        // Drained so the process never blocks on a full pipe, and kept so a start-up failure can
        // report what the application actually said.
        process.OutputDataReceived += (_, args) => Record(args.Data);
        process.ErrorDataReceived += (_, args) => Record(args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private async Task WaitUntilReadyAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (host!.HasExited)
            {
                throw new InvalidOperationException(
                    $"The application exited with code {host.ExitCode} before becoming ready.{Environment.NewLine}{HostOutput()}");
            }

            try
            {
                var response = await client.GetAsync(new Uri(BaseUrl)).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    // The root page answering is not enough. A build started from the wrong
                    // directory serves it happily while every asset behind it is missing.
                    var asset = await client.GetAsync(new Uri($"{BaseUrl}/styles/tokens.css")).ConfigureAwait(false);

                    if (asset.IsSuccessStatusCode)
                    {
                        return;
                    }

                    throw new InvalidOperationException(
                        $"The application is answering but /styles/tokens.css returned {asset.StatusCode}. It is running from {ProjectDirectory()} but its static assets are somewhere else.");
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
            $"The application did not become ready at {BaseUrl} within 90 seconds.{Environment.NewLine}{HostOutput()}");
    }

    private static string ProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CoreRentalNet.sln")))
            {
                return Path.Combine(directory.FullName, "src", "Host", "CoreRentalNet.Host");
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private static string HostAssemblyPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        string? root = null;

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CoreRentalNet.sln")))
            {
                root = directory.FullName;
                break;
            }

            directory = directory.Parent;
        }

        if (root is null)
        {
            throw new InvalidOperationException("Could not locate the repository root.");
        }

        // The build configuration of the tests is the configuration the host was built in.
        var configuration = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            ? "Release"
            : "Debug";

        var hostDll = Path.Combine(
            root, "src", "Host", "CoreRentalNet.Host", "bin", configuration, "net10.0", "CoreRentalNet.Host.dll");

        return File.Exists(hostDll)
            ? hostDll
            : throw new FileNotFoundException($"The application was not built: {hostDll}");
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }
}

[CollectionDefinition(Name)]
public sealed class E2ECollection : ICollectionFixture<HostFixture>
{
    public const string Name = "e2e";
}
