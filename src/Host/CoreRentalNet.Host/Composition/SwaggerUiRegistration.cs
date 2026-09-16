using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using Microsoft.Extensions.DependencyInjection;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// The swagger-ui page, which turns the document into something a person can try.
/// </summary>
/// <remarks>
/// <para>
/// The OAuth settings are the identity configuration, not a second copy of it. The client id is the
/// one the application signs people in with; the scopes are the ones the login asks for, because a
/// permission the provider was not asked for is one it will not issue; and the audience is added to
/// the authorization request, without which the provider issues a token carrying no permissions and
/// the endpoint answers 403. PKCE is on, so no client secret reaches the page.
/// </para>
/// <para>
/// The redirect address is deliberately left unset. swagger-ui derives it from the address the page
/// is served on - <c>oauth2-redirect.html</c> beside itself - so it is right on whatever host and port
/// the application was launched on, with nothing to configure and nothing to keep in step. The one
/// thing that must agree with it is the client's Allowed Callback URL at the provider, which is
/// <c>{address}/swagger/oauth2-redirect.html</c>.
/// </para>
/// </remarks>
internal static class SwaggerUiRegistration
{
    /// <summary>Where the page is served, and the prefix the security headers recognise.</summary>
    public const string RoutePrefix = DocumentationPath.Prefix;

    public static void MapSwaggerUi(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var identity = app.Services.GetRequiredService<IdentitySettings>();

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = RoutePrefix;

            // The document this application actually serves. Swashbuckle's default points at
            // v1/swagger.json, which MapOpenApi does not publish, and a page with no document shows no
            // operations at all - which is exactly what it did until this line existed.
            options.SwaggerEndpoint(DocumentationPath.Document, "CoreRentalNet");

            // The badge fetches a third party, which the policy forbids and the application has no use
            // for; asking for it to be drawn would only paint a broken image. Null is how swagger-ui
            // disables validation.
            options.EnableValidator(null!);

            if (!identity.IsConfigured)
            {
                return;
            }

            options.OAuthClientId(ClientId(app, identity));
            options.OAuthScopes([.. identity.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)]);
            options.OAuthUsePkce();

            if (identity.Audience is { Length: > 0 } audience)
            {
                options.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
                {
                    ["audience"] = audience,
                });
            }
        });
    }

    /// <summary>
    /// The client the page authorizes with: the one the application signs people in with, unless a
    /// deployment names another.
    /// </summary>
    /// <remarks>
    /// The flow is PKCE, which the provider documents for clients that cannot hold a secret - single-page
    /// and native applications - while a regular web application is a confidential client. A deployment
    /// whose provider refuses the exchange without a secret for that client registers a public one for
    /// this page and names it here; everything else stays the same, and the sign-in client is untouched.
    /// </remarks>
    private static string ClientId(WebApplication app, IdentitySettings identity)
        => app.Configuration["Swagger:ClientId"] is { Length: > 0 } configured
            ? configured
            : identity.ClientId!;
}
