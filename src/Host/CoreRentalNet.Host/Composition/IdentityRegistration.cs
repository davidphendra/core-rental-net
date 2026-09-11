using CoreRentalNet.Host.Controllers;
using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace CoreRentalNet.Host.Composition;

/// <summary>
/// Identity, which is optional and additive (ADR-0016). With no domain configured nothing is
/// registered and the application runs exactly as it did before identity existed.
/// </summary>
internal static class IdentityRegistration
{
    public static IdentitySettings AddOptionalIdentity(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = IdentitySettings.From(builder.Configuration);
        builder.Services.AddSingleton(settings);

        if (!settings.IsConfigured)
        {
            return settings;
        }

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
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
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = $"https://{settings.Domain}/";
                options.ClientId = settings.ClientId!;
                options.ClientSecret = settings.ClientSecret;
                options.ResponseType = "code";
                options.UsePkce = true;

                // The default scope for this provider omits the address we record on an order.
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.CallbackPath = AccountController.CallbackPath;

                // Where the provider returns the browser after signing out, which the handler then
                // redirects from. The default is /signout-callback-oidc and has to be registered in
                // the tenant just the same; naming it keeps every identity route under /account.
                options.SignedOutCallbackPath = AccountController.SignedOutCallbackPath;

                // No access token and no refresh token are stored (ADR-0019). The profile is read
                // once at sign-in so the name and email on an order are reliable.
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = true;

                options.TokenValidationParameters.NameClaimType = "name";
            });

        return settings;
    }
}
