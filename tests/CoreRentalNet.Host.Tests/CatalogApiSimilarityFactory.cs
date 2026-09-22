using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The application with the similarityService search's permission configured, so the endpoint can be exercised.
/// </summary>
/// <remarks>
/// <para>
/// The same arrangement <see cref="CatalogApiAuthorizedFactory"/> uses, for the same reason: identity is made
/// configured where the container is final, because the composition root reads identity settings while
/// <c>Program</c> runs - before a test can add configuration - and the policy is re-pointed at
/// <see cref="CatalogApiTestHandler"/> so a principal can be produced without minting a JWT.
/// </para>
/// <para>
/// The vector port is replaced with a stand-in, so what these tests assert is the endpoint and not retrieval:
/// the vectors themselves are exercised in the integration tier over a real file.
/// </para>
/// </remarks>
public sealed class CatalogApiSimilarityFactory : WebApplicationFactory<Program>
{
    private const string ClaimType = "permissions";
    private const string ClaimValue = "searchsimilarity:aibuilder";

    private readonly string _database =
        Path.Combine(Path.GetTempPath(), $"core-rental-api-similarityService-{Guid.NewGuid():N}.db");

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
            var identity = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth0:Enabled"] = "true",
                    ["Auth0:Domain"] = "tenant.example",
                    ["Auth0:ClientId"] = "client",
                    ["Authorization:SimilaritySearch:ClaimType"] = ClaimType,
                    ["Authorization:SimilaritySearch:ClaimValue"] = ClaimValue,
                })
                .Build();

            services.AddSingleton(IdentitySettings.From(identity));
            services.AddSingleton(ClaimSettings.From(identity, ClaimSettings.SimilaritySearch));

            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, CatalogApiTestHandler>(
                CatalogApiTestHandler.SchemeName,
                _ => { });

            services.PostConfigure<AuthorizationOptions>(options => options.AddPolicy(
                SimilaritySearchPolicy.Name,
                policy =>
                {
                    policy.AddRequirements(new ClaimRequirement(
                        ClaimSettings.From(identity, ClaimSettings.SimilaritySearch),
                        UnconfiguredBehaviour.Closed));
                    policy.AddAuthenticationSchemes(CatalogApiTestHandler.SchemeName);
                }));

            services.RemoveAll<IProductSimilarityService>();
            services.AddSingleton<IProductSimilarityService>(provider =>
                new TestProductSimilarityService(provider.GetRequiredService<IProductCatalogService>()));

            // The handler embeds the sentence before it searches, so the search needs one vector. It is fixed
            // and of no consequence to the endpoint: what these tests assert is the endpoint, not retrieval.
            services.RemoveAll<IEmbeddingRepository>();
            services.AddSingleton<IEmbeddingRepository>(new StubEmbeddingRepository());
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
