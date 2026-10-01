using Auth0.AspNetCore.Authentication;
using CoreRentalNet.Host.Components.Pages;
using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;

namespace CoreRentalNet.Host.Extentions;

/// <summary>
/// Identity, which is optional and additive. With no domain configured nothing is
/// registered and the application runs exactly as it did before identity existed.
/// </summary>
/// <remarks>
/// The registration is a list of named concerns - the provider, the access token, the cookie, the
/// handler - rather than one long block, so the concern a reader is looking for is a name they can
/// find instead of a paragraph they have to read.
///
/// "Nothing is registered" means no way in, not no answer at all: a permission that is closed still has
/// to be refused, and refusing is a challenge the framework has to be able to make. What stands in for
/// the missing schemes is <see cref="NoIdentityHandler"/>, and it is the only scheme such a deployment
/// has.
/// </remarks>
internal static class IdentityRegistrationExtentions
{
    public static IdentitySettings AddOptionalIdentity(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = IdentitySettings.From(builder.Configuration);
        var permissionClaim = ClaimSettings.From(builder.Configuration, ClaimSettings.CatalogRead);
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(permissionClaim);

        // The run endpoint's own read of the caller's token, registered whether or not identity is configured.
        // What it reads only exists once the SDK is configured, and a deployment with no identity closes the
        // builder's permission before the endpoint could reach it - so registering it in one branch alone would
        // make the endpoint's dependencies depend on which branch ran.
        builder.Services.AddScoped<ICallerAccessTokenService, CallerAccessTokenService>();

        if (!settings.IsConfigured)
        {
            // Not nothing: a permission that is closed when nobody can be checked still has to answer,
            // and the framework answers by challenging. That needs a scheme to challenge with, and this
            // is the only one such a deployment has. Without it the refusal is an exception - a closed
            // feature that reads to a customer as a broken one.
            builder.Services
                .AddAuthentication(NoIdentityHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, NoIdentityHandler>(
                    NoIdentityHandler.SchemeName,
                    _ => { });

            return settings;
        }

        RegisterProvider(builder, settings);
        RegisterAccessTokenHandler(builder);
        ConfigureCookie(builder);
        ConfigureOpenIdConnect(builder, settings, permissionClaim);

        return settings;
    }

    /// <summary>
    /// Auth0's own package rather than the bare handler it is built on: the scheme name, the
    /// authority, the provider's logout endpoint and the scope the provider recognises are its
    /// conventions, and taking them from the provider is one fewer place to get them wrong. What it
    /// registers under that name is still the standard OpenID Connect handler, so the settings it
    /// does not expose are stated on the handler instead.
    /// </summary>
    private static void RegisterProvider(WebApplicationBuilder builder, IdentitySettings settings)
    {
        builder.Services
            .AddAuth0WebAppAuthentication(options =>
            {
                options.Domain = settings.Domain!;
                options.ClientId = settings.ClientId!;
                options.ClientSecret = settings.ClientSecret;

                // The provider's default scope omits the address we record on an order, and a per-app
                // authorization policy issues a permission only when the login asked for it as a
                // scope. Both are configuration, so the deployment names them beside the audience.
                options.Scope = settings.Scope;

                // The authorization-code flow with PKCE. The wrapper leaves the response type at the
                // handler's default, which is the implicit flow; there is no reason to accept that.
                // It also means a configured deployment without a client secret fails at startup with
                // the wrapper's own message rather than at the first sign-in attempt.
                options.ResponseType = "code";

                // Every identity route stays under /account.
                options.CallbackPath = AccountController.CallbackPath;
            })
            .WithAccessToken(options => ConfigureAccessToken(options, settings));
    }

    /// <summary>How the access token is kept valid: the SDK refreshes it, and its two failure paths end the session.</summary>
    /// <remarks>
    /// <b>Nothing here reads `exp` or compares times.</b> The SDK validates the recorded expiry, exchanges the
    /// refresh token before it lapses, and reports a token or null; this only states the settings and says what
    /// happens when a session can no longer be renewed. Turning refreshing on is also what adds `offline_access`
    /// to the authorize request, which is the only reason Auth0 issues a refresh token at all.
    /// </remarks>
    private static void ConfigureAccessToken(Auth0WebAppWithAccessTokenOptions options, IdentitySettings settings)
    {
        // Auth0 writes an account's permissions onto an access token, and writes one only when the login names an
        // API as the audience. The SDK's own option asks for it; the token it produces is where they are read from.
        options.Audience = settings.Audience;

        options.UseRefreshTokens = true;

        // A suggestion run streams for the better part of a minute, so refresh further ahead than the SDK's
        // 60-second default and never start a run holding a token that lapses mid-run.
        options.AccessTokenExpirationLeeway = TimeSpan.FromMinutes(2);

        options.Events = new Auth0WebAppWithAccessTokenEvents
        {
            OnMissingRefreshToken = SignInAgainAsync,
            OnAccessTokenRefreshFailed = RefreshFailedAsync,
        };
    }

    /// <summary>A session with nothing to refresh with is ended and sent back to sign in.</summary>
    /// <remarks>
    /// A session created before refresh tokens were enabled, or an API without offline access, has no refresh
    /// token - so an expired access token cannot be replaced, and leaving the session looking valid is the
    /// failure this exists to prevent.
    /// </remarks>
    private static async Task SignInAgainAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, SignInProperties());
    }

    /// <summary>A refresh token the provider rejected is terminal, and means the customer signs in again.</summary>
    /// <remarks>
    /// Anything else - a timeout, a rate limit - may succeed on the next attempt, so it is left alone and the
    /// endpoint answers with its own refusal rather than ending a session over a transient fault.
    /// </remarks>
    private static async Task RefreshFailedAsync(AccessTokenRefreshFailedContext context)
    {
        if (context.Error != "invalid_grant")
        {
            return;
        }

        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await context.HttpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, SignInProperties());
    }

    /// <summary>Where a challenge sends a customer whose session has ended.</summary>
    private static AuthenticationProperties SignInProperties()
        => new LoginAuthenticationPropertiesBuilder().WithRedirectUri("/").Build();

    /// <summary>
    /// The SDK keeps that access token in the session. This is what puts it on an outbound request
    /// when the application calls the API the audience names; nothing calls it yet, so it is
    /// registered ready rather than wired to a caller.
    /// </summary>
    private static void RegisterAccessTokenHandler(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<TokenHandler>();
        builder.Services.AddHttpClient(TokenHandler.ClientName).AddHttpMessageHandler<TokenHandler>();
    }

    /// <summary>
    /// The wrapper registers the cookie scheme; the policy it follows is still this application's,
    /// and this states it.
    /// </summary>
    private static void ConfigureCookie(WebApplicationBuilder builder)
    {
        builder.Services.Configure<CookieAuthenticationOptions>(
            CookieAuthenticationDefaults.AuthenticationScheme,
            options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                // Over plain HTTP the cookie cannot be marked Secure, so local development is
                // inherently weaker than production. Production must be HTTPS.
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;

                // Where a challenge sends a customer who is not signed in, and what the return
                // address is called - our own route, so that the middleware's redirect and the links
                // the application writes are the same address twice over rather than two similar ones.
                options.LoginPath = AccountController.SignInPath;
                options.ReturnUrlParameter = "returnUrl";

                // And where a signed-in account that is refused gets sent. The default is a path this
                // application does not serve, which is a blank 404 where an explanation belongs.
                options.AccessDeniedPath = AccessDenied.Path;
            });
    }

    /// <summary>
    /// The handler the wrapper leaves alone: the profile is read once from the user-info endpoint,
    /// the access token is kept, a non-tenant authority is reached through one override, and the
    /// permissions and roles are lifted out of the access token as it is validated.
    /// </summary>
    private static void ConfigureOpenIdConnect(
        WebApplicationBuilder builder,
        IdentitySettings settings,
        ClaimSettings permissionClaim)
    {
        builder.Services
            .AddOptions<OpenIdConnectOptions>(Auth0Constants.AuthenticationScheme)
            .Configure(options =>
            {
                // The profile is read once at sign-in from the user-info endpoint, so the name and
                // email on an order are reliable. The access token the API audience produced is kept
                // in the session: a per-app authorization policy issues a permission only when the
                // login asked for it, and that token is where the permission arrives.
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;

                if (settings.Authority is { Length: > 0 } authority)
                {
                    ConfigureNonTenantAuthority(options, authority);
                }

                // The one thing the ID token never carries. Read from the access token in the same
                // response and kept as claims, so the gate does not depend on the token's lifetime.
                // Chained onto the wrapper's own handler, which runs first.
                options.Events.OnTokenValidated += context =>
                {
                    var accessToken = context.TokenEndpointResponse?.AccessToken;

                    PermissionClaims.AddTo(context.Principal, accessToken, permissionClaim.ClaimType);

                    // The role is stated inside a permission (manager:role, supervisor:role, ...),
                    // so it is read from the same token the permissions arrive on rather than from a
                    // claim an Action has to be written to add.
                    RoleClaims.AddTo(context.Principal, accessToken, settings.RoleClaimType);

                    return Task.CompletedTask;
                };
            });
    }

    /// <summary>
    /// A provider that is not a tenant - the browser suite's own, on localhost over plain HTTP - is
    /// reached through this one override. Left unset the wrapper's domain-derived authority stands,
    /// which is what a real deployment uses.
    /// </summary>
    private static void ConfigureNonTenantAuthority(OpenIdConnectOptions options, string authority)
    {
        options.Authority = authority;
        options.MetadataAddress = $"{authority}/.well-known/openid-configuration";
        options.RequireHttpsMetadata = false;

        // The wrapper pins the issuer to https://{domain}/; the metadata's issuer is this one, and
        // the ID token is validated against it.
        options.TokenValidationParameters.ValidIssuer = authority;

        // The wrapper also replaces sign-out with Auth0's own /v2/logout built from the tenant
        // domain. That is right for a tenant and wrong for any other authority, and it is the one
        // place a stray domain setting could send the browser to a third party. A handler that does
        // nothing leaves the circuit unhandled, so the OpenID Connect handler's own sign-out runs
        // and follows the authority's discovery document instead.
        options.Events.OnRedirectToIdentityProviderForSignOut = _ => Task.CompletedTask;
        options.SignedOutRedirectUri = "/";
    }
}
