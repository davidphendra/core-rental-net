using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// Every permission the application checks, and the one handler that answers them.
/// </summary>
/// <remarks>
/// <para>
/// One handler for four policies, because the rule is the same rule: an account is entitled when it
/// carries the configured claim, and what a deployment with no provider does is the requirement's to
/// say. A handler per permission would be four copies of an exact, case-sensitive comparison, and four
/// copies of a rule is four chances for three of them to be right.
/// </para>
/// <para>
/// The catalogue's two policies answer <c>Open</c> and the AI section's answer <c>Closed</c>. That
/// difference is the whole reason the requirement carries anything: reading the catalogue is served
/// from memory and costs nothing to allow, and generating a suggestion spends model calls and an
/// external round trip.
/// </para>
/// </remarks>
internal static class AuthorizationRegistration
{
    public static void AddCatalogAuthorization(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(CatalogPolicy.Name, policy => policy.AddRequirements(
                Catalogue(builder.Configuration)));

            options.AddPolicy(AiBuilderReadPolicy.Name, policy => policy.AddRequirements(
                Permission(builder.Configuration, ClaimSettings.AiBuilderRead)));

            options.AddPolicy(AiBuilderPowerPolicy.Name, policy => policy.AddRequirements(
                Permission(builder.Configuration, ClaimSettings.AiBuilderPower)));
        });

        builder.Services.AddSingleton<IAuthorizationHandler, ClaimAuthorizationHandler>();
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
                policy.AddRequirements(Catalogue(builder.Configuration));

                if (identity.IsConfigured)
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                }
            }));
    }

    /// <summary>Reading the catalogue: whatever it guards, a demonstration with no provider opens it.</summary>
    private static ClaimRequirement Catalogue(IConfiguration configuration)
        => new(ClaimSettings.From(configuration, ClaimSettings.CatalogRead), ClaimBehavior.Open);

    /// <summary>The AI section's permissions, which spend money and are therefore closed by default.</summary>
    private static ClaimRequirement Permission(IConfiguration configuration, string section)
        => new(ClaimSettings.From(configuration, section), ClaimBehavior.Closed);
}
