using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The application as development runs it, which is where the OpenAPI document is served.
/// </summary>
/// <remarks>
/// The document endpoint is anonymous, so the identity question that makes
/// <see cref="CatalogApiFactory"/> run in the Testing environment does not apply here: this factory
/// needs Development precisely because that is the environment the documentation is published in. The
/// identity settings are replaced with known ones rather than read from the machine, so what the
/// document and the page are configured from does not depend on whose laptop runs the suite. The
/// database is a throwaway file.
/// </remarks>
public sealed class CatalogOpenApiFactory : WebApplicationFactory<Program>
{
    private readonly string _database =
        Path.Combine(Path.GetTempPath(), $"core-rental-openapi-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
        [
            new KeyValuePair<string, string?>("Sqlite:DatabasePath", _database),
            // The similarityService search's permission, so the document test can assert the scope it declares. The
            // deployment's own value; the transformer reads it from configuration rather than naming it.
            new KeyValuePair<string, string?>("Authorization:SimilaritySearch:ClaimType", "permissions"),
            new KeyValuePair<string, string?>("Authorization:SimilaritySearch:ClaimValue", "searchsimilarity:aibuilder"),
        ]));

        builder.ConfigureTestServices(services =>
            services.AddSingleton(SwaggerTestSetup.Identity()));
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
