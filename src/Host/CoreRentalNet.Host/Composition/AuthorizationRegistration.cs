using CoreRentalNet.Host.Infrastructure;
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
}
