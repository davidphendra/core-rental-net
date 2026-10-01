using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Host.Tests.WorkspaceSuggestion;

/// <summary>The application with the suggestion run's permission configured, so the run endpoint requires it.</summary>
/// <remarks>
/// <para>
/// The suggestion permission is <b>closed</b> in the shipped configuration, so a suite that left it alone could
/// only ever observe a refusal. Identity is made configured here, where the container is final, and the policy
/// is re-pointed at <see cref="CatalogApiTestHandler"/> so a principal can be produced without minting a JWT.
/// </para>
/// <para>
/// The environment is not Development, for the reason <see cref="CatalogApiFactory"/> gives: that environment
/// loads <c>appsettings.Local.json</c> and the developer's user secrets, which would decide identity for the
/// suite instead of the suite deciding for itself. No agent is configured, so a run that gets past the gate ends
/// as <c>unavailable</c> rather than answering from memory - which is what makes the streaming assertable
/// without a stand-in.
/// </para>
/// </remarks>
public sealed class SuggestionEndpointFactory : WebApplicationFactory<Program>
{
    private readonly IWorkspaceSuggestionAgentAdapter? _suggestionAgentAdapter;

    /// <summary>No agent, so every run ends unavailable - which is its own assertion.</summary>
    public SuggestionEndpointFactory()
    {
    }

    /// <summary>
    /// A stand-in agent, when a test wants to watch a run produce frames. Internal rather than public because
    /// xUnit's class fixture requires exactly one public constructor, and the parameterless one is what the
    /// suite's fixtures resolve; a test that wants a stand-in builds its own instance.
    /// </summary>
    internal SuggestionEndpointFactory(IWorkspaceSuggestionAgentAdapter suggestionAgentAdapter)
        => _suggestionAgentAdapter = suggestionAgentAdapter;

    private const string ClaimType = "permissions";
    private const string ClaimValue = "builder:ai";

    private readonly string _database =
        Path.Combine(Path.GetTempPath(), $"core-rental-builder-{Guid.NewGuid():N}.db");

    /// <summary>What the application logged, so a test can assert the run's record.</summary>
    internal CapturingLoggerProvider Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("Sqlite:DatabasePath", _database),
        ]));

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILoggerProvider>(Logs);

            if (_suggestionAgentAdapter is not null)
            {
                services.RemoveAll<IWorkspaceSuggestionAgentAdapter>();
                services.AddSingleton(_suggestionAgentAdapter);
            }

            var identity = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth0:Enabled"] = "true",
                    ["Auth0:Domain"] = "tenant.example",
                    ["Auth0:ClientId"] = "client",
                    ["Authorization:WorkspaceSuggestion:ClaimType"] = ClaimType,
                    ["Authorization:WorkspaceSuggestion:ClaimValue"] = ClaimValue,
                })
                .Build();

            services.AddSingleton(IdentitySettings.From(identity));
            services.AddSingleton(ClaimSettings.From(identity, ClaimSettings.WorkspaceSuggestion));

            // The endpoints read the caller's token through the identity SDK, and the SDK authenticates its own
            // cookie scheme and reads the ticket's recorded expiry. So the test sign-in is made to look like an
            // SDK sign-in: the SDK's cookie scheme is named as the test scheme, and the options it asks for are
            // configured here. No refresh is reachable - the ticket's expiry is in the future - so nothing tries
            // to sign the test client in again.
            services.Configure<Auth0WebAppOptions>(Auth0Constants.AuthenticationScheme, options =>
            {
                options.Domain = "tenant.example";
                options.ClientId = "client";
                options.CookieAuthenticationScheme = CatalogApiTestHandler.SchemeName;
            });

            services.Configure<Auth0WebAppWithAccessTokenOptions>(Auth0Constants.AuthenticationScheme, options =>
            {
                options.Audience = "https://corerental/api";
                options.UseRefreshTokens = true;

                // A null answer is asserted as the endpoint's own refusal rather than as a sign-out, so the
                // events that would end a real session are left unset in a test.
                options.Events = null;
            });

            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, CatalogApiTestHandler>(
                CatalogApiTestHandler.SchemeName,
                _ => { });

            // The run endpoint reads the caller's access token from the default sign-in, so the test sign-in has to
            // be that default: otherwise the token the ticket carries is never found and every run is refused.
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = CatalogApiTestHandler.SchemeName;
                options.DefaultChallengeScheme = CatalogApiTestHandler.SchemeName;
            });

            // Re-pointed rather than left as the composition root built it: that policy is built from the
            // deployment's configuration, which is blank here, and a blank permission means nobody.
            services.PostConfigure<AuthorizationOptions>(options => options.AddPolicy(
                WorkspaceSuggestionPolicy.Name,
                policy =>
                {
                    policy.AddRequirements(new ClaimRequirement(
                        ClaimSettings.From(identity, ClaimSettings.WorkspaceSuggestion),
                        UnconfiguredBehaviour.Closed));
                    policy.AddAuthenticationSchemes(CatalogApiTestHandler.SchemeName);
                }));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        foreach (var file in new[] { _database, $"{_database}-wal", $"{_database}-shm" })
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
