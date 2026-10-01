using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CoreRentalNet.Host.Tests.Identity;

/// <summary>The application with a configured identity provider, so the SDK's own options can be read back.</summary>
/// <remarks>
/// The identity settings are added to configuration <b>before</b> the composition root runs, which is what makes
/// the real registration happen: a factory that only replaced the <c>IdentitySettings</c> singleton afterwards
/// would leave the SDK unregistered. No provider is reached - the options are read without a sign-in - so this
/// never touches a network.
/// </remarks>
public sealed class Auth0IdentityFactory : WebApplicationFactory<Program>
{
    private readonly string _database =
        Path.Combine(Path.GetTempPath(), $"core-rental-auth0-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        // Stated as host settings rather than as an added configuration source: the composition root reads
        // `builder.Configuration` while it is still building, and host settings are what reaches it there.
        builder.UseSetting("Sqlite:DatabasePath", _database);
        builder.UseSetting("Auth0:Enabled", "true");
        builder.UseSetting("Auth0:Domain", "tenant.example");
        builder.UseSetting("Auth0:ClientId", "client");
        builder.UseSetting("Auth0:ClientSecret", "client-secret");
        builder.UseSetting("Auth0:Audience", "https://corerental/api");
        builder.UseSetting("Auth0:LeewaySeconds", "180");
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
