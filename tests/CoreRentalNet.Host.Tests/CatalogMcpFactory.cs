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
/// The application with identity and both catalogue permissions configured, so the MCP tools can be driven
/// through the real endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The same arrangement <see cref="CatalogApiSimilarityFactory"/> uses, for the same reason: the composition
/// root reads identity settings while <c>Program</c> runs, so identity is made configured here where the
/// container is final, and every policy the MCP surface consults is re-pointed at
/// <see cref="CatalogApiTestHandler"/> so a principal can be produced without minting a JWT.
/// </para>
/// <para>
/// The vector port is a stand-in, so what these tests assert is the surface - what is discovered, what is
/// refused, and the shape of the answer - and not retrieval, which is exercised over a real file elsewhere.
/// </para>
/// </remarks>
public class CatalogMcpFactory : WebApplicationFactory<Program>
{
    private const string ClaimType = "permissions";

    private readonly string _database =
        Path.Combine(Path.GetTempPath(), $"core-rental-mcp-{Guid.NewGuid():N}.db");

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
            var identity = Identity();

            services.AddSingleton(IdentitySettings.From(identity));
            services.AddSingleton(ClaimSettings.From(identity, ClaimSettings.CatalogRead));
            services.AddSingleton(ClaimSettings.From(identity, ClaimSettings.SimilaritySearch));

            services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, CatalogApiTestHandler>(
                CatalogApiTestHandler.SchemeName,
                _ => { });

            services.PostConfigure<AuthorizationOptions>(options =>
            {
                options.AddPolicy(McpPolicy.Name, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddAuthenticationSchemes(CatalogApiTestHandler.SchemeName);
                });

                options.AddPolicy(CatalogApiPolicy.Name, policy =>
                {
                    policy.AddRequirements(new ClaimRequirement(
                        ClaimSettings.From(identity, ClaimSettings.CatalogRead),
                        UnconfiguredBehaviour.Open));
                    policy.AddAuthenticationSchemes(CatalogApiTestHandler.SchemeName);
                });

                options.AddPolicy(SimilaritySearchPolicy.Name, policy =>
                {
                    policy.AddRequirements(new ClaimRequirement(
                        ClaimSettings.From(identity, ClaimSettings.SimilaritySearch),
                        UnconfiguredBehaviour.Closed));
                    policy.AddAuthenticationSchemes(CatalogApiTestHandler.SchemeName);
                });
            });

            services.RemoveAll<IProductSimilarityService>();
            services.AddSingleton<IProductSimilarityService>(provider =>
                Similarity(provider.GetRequiredService<IProductCatalogService>()));

            services.RemoveAll<IEmbeddingService>();
            services.AddSingleton<IEmbeddingService>(new StubEmbeddingService());
        });
    }

    /// <summary>The similarity port the tools answer through, which a subclass may make fail.</summary>
    protected virtual IProductSimilarityService Similarity(IProductCatalogService catalogue)
        => new TestProductSimilarityService(catalogue);

    private static IConfiguration Identity()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth0:Enabled"] = "true",
                ["Auth0:Domain"] = "tenant.example",
                ["Auth0:ClientId"] = "client",
                ["Authorization:CatalogRead:ClaimType"] = ClaimType,
                ["Authorization:CatalogRead:ClaimValue"] = "read:catalog",
                ["Authorization:SimilaritySearch:ClaimType"] = ClaimType,
                ["Authorization:SimilaritySearch:ClaimValue"] = "searchsimilarity:aibuilder",
            })
            .Build();

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
