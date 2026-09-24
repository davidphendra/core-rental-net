using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// Every permission the application checks, and the one handler that answers them.
/// </summary>
/// <remarks>
/// One handler for every policy, because the rule is the same rule: an account is entitled when it
/// carries the configured claim. A handler per permission would be two copies of an exact,
/// case-sensitive comparison, and two copies of a rule are two chances for one of them to be right.
///
/// Each permission's own registration adds the handler, so it is there whichever of them a composition
/// root calls. That is deliberately <b>not</b> <c>TryAddSingleton</c>: that would be keyed on
/// <see cref="IAuthorizationHandler"/>, which the identity packages also register implementations of, so
/// this application's handler would silently not be added and every permission would deny. Tried, and the
/// catalogue's own tests caught it. A second instance is harmless — both apply the same rule — while a
/// missing one denies everyone.
/// </remarks>
internal static class AuthorizationRegistrationExtentions
{
    public static void AddCatalogAuthorization(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAuthorization(options => options.AddPolicy(
            CatalogPolicy.Name,
            policy => policy.AddRequirements(Catalogue(builder.Configuration))));

        builder.Services.AddSingleton<IAuthorizationHandler, ClaimAuthorizationHandler>();
    }

    /// <summary>
    /// The permission that guards the AI workspace builder, closed when the deployment has not configured it.
    /// </summary>
    /// <remarks>
    /// <b>Closed</b>, because a suggestion run is a paid model call: a deployment that has not said which claim
    /// entitles a customer has not been finished, and the safe reading of an unfinished rule is nobody. The
    /// page hides the section for the same reason, but hiding a section is presentation rather than
    /// authorisation - the endpoint checks this policy for itself.
    /// </remarks>
    public static void AddAiAuthorization(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAuthorization(options => options.AddPolicy(
            AiPolicy.Name,
            policy => policy.AddRequirements(new ClaimRequirement(
                ClaimSettings.From(builder.Configuration, ClaimSettings.AiUse),
                UnconfiguredBehaviour.Closed))));

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

    /// <summary>
    /// The permission that guards the catalogue's similarityService search.
    /// </summary>
    /// <remarks>
    /// <b>Closed</b>, because a similarity search embeds the caller's sentence against a deployment's server,
    /// so a deployment that cannot say which claim entitles a caller has not been finished. It is a separate
    /// policy from the catalogue's own read because the two capabilities are asked for separately.
    /// </remarks>
    public static void AddSimilaritySearchAuthorization(this WebApplicationBuilder builder, IdentitySettings identity)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(identity);

        builder.Services.AddAuthorization(options => options.AddPolicy(
            SimilaritySearchPolicy.Name,
            policy =>
            {
                policy.AddRequirements(new ClaimRequirement(
                    ClaimSettings.From(builder.Configuration, ClaimSettings.SimilaritySearch),
                    UnconfiguredBehaviour.Closed));

                if (identity.IsConfigured)
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                }
            }));

        builder.Services.AddSingleton<IAuthorizationHandler, ClaimAuthorizationHandler>();
    }

    /// <summary>
    /// The gate in front of the MCP endpoint: an authenticated caller, and nothing more.
    /// </summary>
    /// <remarks>
    /// Which tools the caller may use is decided per tool, so this policy carries no claim of its own. The
    /// bearer scheme is named only where one is registered, for the reason the API's own policy gives: a policy
    /// that names a scheme nobody registered throws when authorization runs.
    /// </remarks>
    public static void AddCatalogMcpAuthorization(this WebApplicationBuilder builder, IdentitySettings identity)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(identity);

        builder.Services.AddAuthorization(options => options.AddPolicy(
            McpPolicy.Name,
            policy =>
            {
                policy.RequireAuthenticatedUser();

                if (identity.IsConfigured)
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                }
            }));
    }

    /// <summary>Reading the catalogue: whatever it guards, a demonstration with no provider opens it.</summary>
    private static ClaimRequirement Catalogue(IConfiguration configuration)
        => new(
            ClaimSettings.From(configuration, ClaimSettings.CatalogRead),
            UnconfiguredBehaviour.Open);
}
