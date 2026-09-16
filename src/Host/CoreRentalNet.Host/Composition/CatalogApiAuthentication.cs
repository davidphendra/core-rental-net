using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The scheme a machine caller authenticates with: a bearer access token from the provider.
/// </summary>
/// <remarks>
/// Registered only when there is a provider, because with none there is nothing to validate a token
/// against and the catalogue policy opens the endpoint instead. The browser's cookie stays the default
/// scheme: bearer is named by the API policy alone, so a person signing in is unaffected.
/// </remarks>
internal static class CatalogApiAuthentication
{
    public static void AddCatalogApiAuthentication(this WebApplicationBuilder builder, IdentitySettings identity)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(identity);

        if (!identity.IsConfigured)
        {
            return;
        }

        builder.Services
            .AddAuthentication()
            .AddJwtBearer(options => Configure(options, identity));
    }

    /// <summary>
    /// The provider's authority, and the API the token is for. A non-tenant authority - the browser
    /// suite's own, on localhost over plain HTTP - is reached through the same override identity
    /// already uses.
    /// </summary>
    private static void Configure(JwtBearerOptions options, IdentitySettings identity)
    {
        if (identity.Authority is { Length: > 0 } authority)
        {
            options.Authority = authority;
            options.MetadataAddress = $"{authority}/.well-known/openid-configuration";
            options.RequireHttpsMetadata = false;
        }
        else
        {
            options.Authority = $"https://{identity.Domain}/";
        }

        // The API the access token is for. Absent, the issuer is checked and the audience is not.
        options.Audience = identity.Audience;
    }
}
