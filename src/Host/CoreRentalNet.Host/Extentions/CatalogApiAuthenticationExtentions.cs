using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// The scheme a machine caller authenticates with: a bearer access token from the provider.
/// </summary>
/// <remarks>
/// Registered whenever there is a provider, so a machine caller is always refused by the bearer scheme
/// - a challenge that answers 401 - rather than being handed the browser's cookie challenge and
/// redirected to a sign-in page. What a token must carry is made explicit in <see cref="Configure"/>.
/// The cookie stays the default scheme: bearer is named by the API policy alone.
/// </remarks>
internal static class CatalogApiAuthenticationExtentions
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
            .AddJwtBearer(options => Configure(options, identity, builder.Environment));
    }

    /// <summary>
    /// The provider's authority, and the API the token is for.
    /// </summary>
    /// <remarks>
    /// A non-tenant authority - the browser suite's own, on localhost over plain HTTP - is reached at
    /// whatever address it names, and only in development may that be HTTP; anywhere else the
    /// discovery document must come over HTTPS. The metadata address is deliberately not set: the
    /// handler derives it from the authority, and writing it here as well was a second copy of the
    /// same rule waiting to disagree with the first.
    /// </remarks>
    private static void Configure(JwtBearerOptions options, IdentitySettings identity, IWebHostEnvironment environment)
    {
        if (identity.Authority is { Length: > 0 } authority)
        {
            options.Authority = authority;
            options.RequireHttpsMetadata = !environment.IsDevelopment();
        }
        else
        {
            options.Authority = $"https://{identity.Domain}/";
        }

        // The API the access token is for. This is what binds a token to *this* API rather than to
        // whatever else the same provider issued, so the requirement is stated rather than left to a
        // default: with no audience configured, every token is refused instead of being accepted on
        // its issuer alone.
        options.Audience = identity.Audience;
        options.TokenValidationParameters.RequireAudience = true;
    }
}
