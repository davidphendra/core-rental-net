using Microsoft.Playwright;
using Xunit;

namespace CoreRentalNet.E2E;

/// <summary>
/// A real application, started as a real process, on a real port, with its own database file.
/// </summary>
/// <remarks>
/// <para>
/// Two hosts run, because identity is optional: the guest host has it off, so the funnel
/// is exercised exactly as it ships without a provider, and the authenticated host has it on and
/// points at the local provider in this repository, so a real OIDC handshake can be
/// driven and the builder's gate can be observed from the browser.
/// </para>
/// <para>
/// Started once for the whole collection. Isolation between tests comes from a fresh browser context
/// per test, which gives each test its own draft cookie and therefore its own workspace.
/// </para>
/// <para>
/// This is the composition: which hosts exist, and when each starts. Starting a process, waiting for
/// one, and finding the assembly to start belong to <see cref="ProcessPool"/> and
/// <see cref="TestPaths"/>.
/// </para>
/// </remarks>
public sealed class HostFixture : IAsyncLifetime
{
    private readonly ProcessPool processes = new();
    private string? workingDirectory;
    private string? realTenantBaseUrl;

    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>The host with identity on, pointed at <see cref="ProviderAuthority"/>.</summary>
    public string AuthenticatedBaseUrl { get; private set; } = string.Empty;

    /// <summary>The local provider's issuer address, for tests that assert where a redirect went.</summary>
    public string ProviderAuthority { get; private set; } = string.Empty;

    public IPlaywright Playwright { get; private set; } = null!;

    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Where the databases go. A throwaway directory, so a run cannot touch real data.
        workingDirectory = Path.Combine(Path.GetTempPath(), $"core-rental-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);

        // The stand-in agent first, so both hosts are configured with its address rather than
        // discovering it: an application told where the agent is at start-up is the deployment this
        // suite is meant to be running.
        var agentPort = TestPaths.FreePort();
        AgentUrl = $"http://127.0.0.1:{agentPort}";
        var agent = StartLocalAgent(AgentUrl);

        await ProcessPool.WaitUntilAgentReadyAsync(AgentUrl, agent).ConfigureAwait(false);

        var port = TestPaths.FreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var guest = StartHost(
            port,
            "e2e.db",
            new Dictionary<string, string>
            {
                // Identity off, explicitly and by configuration rather than by having no credentials.
                // Development loads appsettings.Local.json, that file is loaded last so it wins over
                // the environment, and on a machine that has real Auth0 credentials the catalog gate
                // would then be live for every test - which is how nine of them failed before this
                // line existed. The suite has to run the application the way it ships, not the way
                // one laptop is configured.
                ["Auth0__Enabled"] = "false",
                // The stand-in, reached over http with no credential: the agent's local path, which is
                // the whole reason the page can be exercised without a subscription.
                ["Agent__Endpoint"] = AgentUrl,
                ["Agent__Name"] = "stand-in",
                // The cooldown is off for the suite, and deliberately: it is measured from when a run
                // starts, so one test's run would refuse the next test's - and every AI test signs in as
                // the same account. The guard's one-in-flight rule is the part the browser holds; the
                // cooldown has its own unit tests with values it chooses.
                ["Ai__RunCooldownSeconds"] = "0",
            });

        await ProcessPool.WaitUntilReadyAsync(BaseUrl, guest, requireAssets: true).ConfigureAwait(false);

        // The provider first, so its authority is known when the authenticated host is configured.
        var providerPort = TestPaths.FreePort();
        var provider = StartLocalProvider(providerPort);

        await ProcessPool.WaitUntilProviderReadyAsync(ProviderAuthority, provider).ConfigureAwait(false);

        var authenticatedPort = TestPaths.FreePort();
        AuthenticatedBaseUrl = $"http://127.0.0.1:{authenticatedPort}";

        var authenticated = StartHost(
            authenticatedPort,
            "e2e-auth.db",
            new Dictionary<string, string>
            {
                ["Auth0__Enabled"] = "true",
                // The wrapper needs a domain to build its default authority from, and refuses a
                // code-flow registration without a secret. Both are overridden or ignored by the
                // provider; the authority below is what actually decides where the handshake goes.
                ["Auth0__Domain"] = "local-provider",
                ["Auth0__ClientId"] = "core-rental-e2e",
                ["Auth0__ClientSecret"] = "local-provider-secret",
                ["Auth0__Authority"] = ProviderAuthority,
                ["Agent__Endpoint"] = AgentUrl,
                ["Agent__Name"] = "stand-in",
                ["Ai__RunCooldownSeconds"] = "0",
            });

        await ProcessPool.WaitUntilReadyAsync(AuthenticatedBaseUrl, authenticated, requireAssets: true).ConfigureAwait(false);

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

        await processes.DisposeAsync().ConfigureAwait(false);

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

    private AppProcess StartHost(int port, string databaseName, Dictionary<string, string> environment, string host = "127.0.0.1")
        => processes.Start(
            TestPaths.HostAssembly(),
            // Started from the application's own directory, exactly as dotnet run does. A development
            // build serves its static assets, its fonts and its vendored product images from there, so
            // starting it anywhere else loses all of them silently.
            TestPaths.ProjectDirectory(),
            new Dictionary<string, string>(environment)
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ASPNETCORE_URLS"] = $"http://{host}:{port}",
                // The database is a throwaway file in a throwaway directory, so a run cannot
                // contaminate the developer's own data and cannot be contaminated by it.
                ["Sqlite__DatabasePath"] = Path.Combine(workingDirectory!, databaseName),
                ["Rentals__SchedulerIntervalMinutes"] = "1",
                ["Logging__LogLevel__Default"] = "Warning",
            });

    /// <summary>Where the stand-in agent is, for the tests that choose which scenario it acts.</summary>
    public string AgentUrl { get; private set; } = null!;

    /// <summary>
    /// The stand-in agent, started as a process so the page reaches it over a socket exactly as it would
    /// reach a deployed one.
    /// </summary>
    /// <remarks>
    /// In process for the tests that exercise the client, and as a process here, because the page's tests
    /// must go through a real request: an agent that answered in the same process could hide a client
    /// that never left it.
    /// </remarks>
    private AppProcess StartLocalAgent(string baseUrl)
        => processes.Start(
            TestPaths.LocalAgentAssembly(),
            TestPaths.RepositoryRoot(),
            new Dictionary<string, string>
            {
                ["ASPNETCORE_URLS"] = baseUrl,
                ["Logging__LogLevel__Default"] = "Warning",
            });

    private AppProcess StartLocalProvider(int port)
    {
        ProviderAuthority = $"http://127.0.0.1:{port}";

        return processes.Start(
            TestPaths.LocalProviderAssembly(),
            TestPaths.RepositoryRoot(),
            new Dictionary<string, string>
            {
                // The provider refuses to run anywhere else; this is the one place that asks it to.
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ASPNETCORE_URLS"] = ProviderAuthority,
                ["Logging__LogLevel__Default"] = "Warning",
            });
    }

    /// <summary>
    /// The host with identity on and pointed at the developer's real tenant, for the opt-in test.
    /// </summary>
    /// <remarks>
    /// Started only when that test runs, on the one address the README registers with the tenant
    /// (<c>http://localhost:5199</c>), because the callback URL has to match character for
    /// character. The tenant's credentials come from <c>appsettings.Local.json</c>, which the host
    /// loads in Development exactly as it would for a person; the API whose access token carries
    /// the permission comes from <c>CORERENTAL_TENANT_AUDIENCE</c> when there is one.
    /// </remarks>
    public async Task<string> StartRealTenantHostAsync()
    {
        if (realTenantBaseUrl is not null)
        {
            return realTenantBaseUrl;
        }

        // A developer's own `dotnet run` may already own the one address the tenant's callback is
        // registered for. Reusing it is kinder than killing it, and the test wants a running host
        // rather than a particular process.
        if (Environment.GetEnvironmentVariable("CORERENTAL_TENANT_URL") is { Length: > 0 } running)
        {
            realTenantBaseUrl = running.TrimEnd('/');

            return realTenantBaseUrl;
        }

        const int port = 5199;

        var environment = new Dictionary<string, string>
        {
            ["Auth0__Enabled"] = "true",
        };

        // Without an API audience Auth0 issues no JWT access token, so no permission claim can be
        // read and the builder is refused. The variable lets the test say which case it is in.
        if (Environment.GetEnvironmentVariable("CORERENTAL_TENANT_AUDIENCE") is { Length: > 0 } audience)
        {
            environment["Auth0__Audience"] = audience;
        }

        // A deployment may entitle readers by a claim the tenant already issues - a role, for
        // instance - rather than by an API permission it cannot issue. The gate is configuration,
        // so the probe can be pointed at whichever claim is being checked.
        if (Environment.GetEnvironmentVariable("CORERENTAL_TENANT_CLAIM_TYPE") is { Length: > 0 } claimType)
        {
            environment["Authorization__CatalogRead__ClaimType"] = claimType;
        }

        if (Environment.GetEnvironmentVariable("CORERENTAL_TENANT_CLAIM_VALUE") is { Length: > 0 } claimValue)
        {
            environment["Authorization__CatalogRead__ClaimValue"] = claimValue;
        }

        var host = StartHost(port, "e2e-real-tenant.db", environment, host: "localhost");

        realTenantBaseUrl = $"http://localhost:{port}";

        await ProcessPool.WaitUntilReadyAsync(realTenantBaseUrl, host, requireAssets: true).ConfigureAwait(false);

        return realTenantBaseUrl;
    }
}
