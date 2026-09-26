using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, CatalogApiTestHandler>(
                CatalogApiTestHandler.SchemeName,
                _ => { });

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
