using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The catalog policy, and who answers it.
/// </summary>
internal static class AuthorizationRegistration
{
    public static void AddCatalogAuthorization(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAuthorization(options => options.AddPolicy(
            CatalogPolicy.Name,
            policy => policy.AddRequirements(new CatalogReadRequirement())));

        builder.Services.AddSingleton<IAuthorizationHandler, CatalogReadAuthorizationHandler>();
    }

    /// <summary>
    /// The same requirement, for a machine caller.
    /// </summary>
    /// <remarks>
    /// The bearer scheme is named only where one is registered, which is wherever there is a provider:
    /// a policy that names a scheme nobody registered throws when authorization runs. With no provider
    /// the requirement opens the catalogue by itself, exactly as it does for the pages.
    /// </remarks>
    public static void AddCatalogApiAuthorization(this WebApplicationBuilder builder, IdentitySettings identity)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(identity);

        builder.Services.AddAuthorization(options => options.AddPolicy(
            CatalogApiPolicy.Name,
            policy =>
            {
                policy.AddRequirements(new CatalogReadRequirement());

                if (identity.IsConfigured)
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                }
            }));
    }
}
