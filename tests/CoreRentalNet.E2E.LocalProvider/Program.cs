using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using CoreRentalNet.E2E.LocalProvider;

// The browser suite's identity provider: just enough OpenID Connect for a real authorization-code
// handshake with PKCE, on localhost, in this repository. The application points at it through the
// ordinary Authority setting, so nothing between the browser and the servers is intercepted
//. It is a demonstration of a provider, not a provider: no persistence, no consent, and accounts fixed in
// code. It does issue and honour refresh tokens, because the application's session depends on them.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Refused outside Development, and refused by failing the boot rather than by a convention, so a
// stray setting cannot stand it up where someone might believe it. The browser suite starts it with
// ASPNETCORE_ENVIRONMENT=Development; anything else is a mistake and stops here.
if (!app.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "The local identity provider is for the browser suite only and refuses to run outside Development. " +
        "Set ASPNETCORE_ENVIRONMENT=Development to start it.");
}

using var rsa = RSA.Create(2048);
var keyId = Base64UrlEncoder.Encode(SHA256.HashData(rsa.ExportRSAPublicKey()))[..16];
var credentials = new SigningCredentials(
    new RsaSecurityKey(rsa) { KeyId = keyId },
    SecurityAlgorithms.RsaSha256);
var tokens = new JsonWebTokenHandler();

// How long an access token the provider issues is valid, and the refresh tokens it has handed out. The lifetime
// is configurable so the browser suite can make it short enough that the identity SDK refreshes within its own
// leeway - which is the path that keeps a session usable after the first expiry, and the one worth exercising.
var accessTokenLifetimeSeconds =
    int.TryParse(Environment.GetEnvironmentVariable("ACCESS_TOKEN_TTL_SECONDS"), out var configuredLifetime)
    && configuredLifetime > 0
        ? configuredLifetime
        : 3600;

var refreshTokens = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

// Where the provider puts a role, matching the application's default. The builder's gate reads a
// permission, not this; the profile page reads the role.
const string RoleClaimType = "https://core-rental.periang.auth0/roles";

var accounts = new Dictionary<string, Account>(StringComparer.Ordinal)
{
    // Holds the permission the builder's gate requires, and the role twice: the Action writes it
    // into the ID token and the permission's name states it too. It also holds the AI permission,
    // because it is the account the browser tier drives the suggestion run as.
    ["reader"] = new("auth0|reader", "Dewi Reader", "dewi@example.com", ["read:catalog", "use:ai", "manager:role"], ["Manager"]),
    ["builder"] = new("auth0|builder", "Sari Builder", "sari@example.com", ["read:catalog"], []),
    ["viewer"] = new("auth0|viewer", "Andi Viewer", "andi@example.com", ["read:catalog"], []),
    // Signed in, but entitled to nothing.
    ["guest"] = new("auth0|guest", "Bagus Guest", "bagus@example.com", [], []),
    // Holds the gate's words in the other order, so it is refused. Its role is named only inside
    // the access token's permission, so the profile proves the role is read from there.
    ["reversed"] = new("auth0|reversed", "Citra Reversed", "citra@example.com", ["catalog:read", "supervisor:role"], []),
};

var pending = new ConcurrentDictionary<string, PendingCode>(StringComparer.Ordinal);

// ---------------------------------------------------------------- discovery

app.MapGet("/.well-known/openid-configuration", (HttpRequest request) =>
{
    var authority = Authority(request);

    return Results.Json(new Dictionary<string, object>
    {
        ["issuer"] = authority,
        ["authorization_endpoint"] = $"{authority}/authorize",
        ["token_endpoint"] = $"{authority}/oauth/token",
        ["userinfo_endpoint"] = $"{authority}/userinfo",
        ["jwks_uri"] = $"{authority}/.well-known/jwks.json",
        ["end_session_endpoint"] = $"{authority}/logout",
        ["response_types_supported"] = new[] { "code" },
        ["response_modes_supported"] = new[] { "query" },
        ["grant_types_supported"] = new[] { "authorization_code", "refresh_token" },
        ["subject_types_supported"] = new[] { "public" },
        ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
        ["scopes_supported"] = new[] { "openid", "profile", "email", "offline_access" },
        ["claims_supported"] = new[] { "sub", "name", "email", "nonce" },
        ["code_challenge_methods_supported"] = new[] { "S256" },
    });
});

app.MapGet("/.well-known/jwks.json", () =>
{
    var parameters = rsa.ExportParameters(includePrivateParameters: false);

    return Results.Json(new
    {
        keys = new[]
        {
            new
            {
                kty = "RSA",
                use = "sig",
                kid = keyId,
                alg = "RS256",
                n = Base64UrlEncoder.Encode(parameters.Modulus!),
                e = Base64UrlEncoder.Encode(parameters.Exponent!),
            },
        },
    });
});

// ---------------------------------------------------------------- authorization

app.MapGet("/authorize", (HttpRequest request) =>
{
    var query = request.Query;
    var account = query["account"].ToString();

    // The application's challenge carries no account, so the provider asks. The links keep every
    // parameter the application sent and add the choice, which is the whole of this page's job.
    if (string.IsNullOrEmpty(account) || !accounts.ContainsKey(account))
    {
        return Results.Content(LoginPage(request.QueryString.Value, accounts), "text/html");
    }

    var clientId = query["client_id"].ToString();
    var redirectUri = query["redirect_uri"].ToString();
    var state = query["state"].ToString();
    var nonce = query["nonce"].ToString();
    var codeChallenge = query["code_challenge"].ToString();
    var codeChallengeMethod = query["code_challenge_method"].ToString();
    var scope = query["scope"].ToString();

    if (!string.Equals(query["response_type"], "code", StringComparison.Ordinal)
        || string.IsNullOrEmpty(clientId)
        || string.IsNullOrEmpty(redirectUri))
    {
        return Results.BadRequest(new { error = "invalid_request" });
    }

    var code = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(24));

    pending[code] = new PendingCode(
        clientId,
        redirectUri,
        account,
        nonce,
        codeChallenge,
        codeChallengeMethod,
        scope,
        DateTimeOffset.UtcNow.AddMinutes(5));

    var separator = redirectUri.Contains('?', StringComparison.Ordinal) ? '&' : '?';

    return Results.Redirect(
        $"{redirectUri}{separator}code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(state)}");
});

// ---------------------------------------------------------------- tokens

app.MapPost("/oauth/token", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);
    var authority = Authority(request);

    // The refresh grant, which the identity SDK exchanges whenever the access token has lapsed. The refresh
    // token is an opaque value this provider keeps in memory, and it is rotated on every exchange.
    if (string.Equals(form["grant_type"].ToString(), "refresh_token", StringComparison.Ordinal))
    {
        var presented = form["refresh_token"].ToString();

        if (string.IsNullOrEmpty(presented) || !refreshTokens.TryRemove(presented, out var subject))
        {
            return Results.BadRequest(new { error = "invalid_grant" });
        }

        var refreshed = accounts.Values.FirstOrDefault(
            candidate => string.Equals(candidate.Subject, subject, StringComparison.Ordinal));

        if (refreshed is null)
        {
            return Results.BadRequest(new { error = "invalid_grant" });
        }

        return Results.Json(TokenResponseFor(
            authority,
            form["client_id"].ToString(),
            refreshed,
            nonce: null,
            scope: "openid profile email offline_access"));
    }

    var code = form["code"].ToString();

    if (string.IsNullOrEmpty(code) || !pending.TryRemove(code, out var issued))
    {
        return Results.BadRequest(new { error = "invalid_grant" });
    }

    if (issued.ExpiresAt < DateTimeOffset.UtcNow
        || !PkceMatches(issued.CodeChallenge, issued.CodeChallengeMethod, form["code_verifier"].ToString()))
    {
        return Results.BadRequest(new { error = "invalid_grant" });
    }

    var account = accounts[issued.Account];

    return Results.Json(TokenResponseFor(authority, issued.ClientId, account, issued.Nonce, issued.Scope));
});

app.MapGet("/userinfo", (HttpRequest request) =>
{
    var bearer = Bearer(request);

    if (bearer is null)
    {
        return Results.Unauthorized();
    }

    var subject = new JsonWebToken(bearer).GetClaim("sub")?.Value;
    var account = accounts.Values.FirstOrDefault(candidate => candidate.Subject == subject);

    if (account is null)
    {
        return Results.Unauthorized();
    }

    return Results.Json(new Dictionary<string, object>
    {
        ["sub"] = account.Subject,
        ["name"] = account.Name,
        ["email"] = account.Email,
    });
});

// Signing out: the application hands the provider a return address and the browser is sent back
// there. There is no provider session to end, so that is all there is to do - but the state the
// application protected its properties with has to come back with it, or the application cannot
// tell where to land afterwards.
app.MapGet("/logout", (HttpRequest request) =>
{
    var returnTo = request.Query["post_logout_redirect_uri"].ToString();

    if (string.IsNullOrEmpty(returnTo))
    {
        return Results.Redirect("/");
    }

    if (request.Query["state"].ToString() is { Length: > 0 } state)
    {
        var separator = returnTo.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        returnTo = $"{returnTo}{separator}state={Uri.EscapeDataString(state)}";
    }

    return Results.Redirect(returnTo);
});

await app.RunAsync();

// ---------------------------------------------------------------- helpers

string Authority(HttpRequest request) => $"{request.Scheme}://{request.Host}";

string CreateToken(string issuer, string audience, Account account, string? nonce, bool accessToken)
{
    var claims = new Dictionary<string, object>
    {
        ["sub"] = account.Subject,
        ["name"] = account.Name,
        ["email"] = account.Email,
    };

    if (nonce is { Length: > 0 })
    {
        claims["nonce"] = nonce;
    }

    if (accessToken)
    {
        // Auth0 writes an array, and the application reads every element.
        if (account.Permissions.Length > 0)
        {
            claims["permissions"] = account.Permissions;
        }
    }
    else if (account.Roles.Length > 0)
    {
        claims[RoleClaimType] = account.Roles;
    }

    return tokens.CreateToken(new SecurityTokenDescriptor
    {
        Issuer = issuer,
        Audience = audience,
        Claims = claims,
        IssuedAt = DateTime.UtcNow,
        Expires = DateTime.UtcNow.AddSeconds(accessTokenLifetimeSeconds),
        SigningCredentials = credentials,
    });
}

/// <summary>The token response for one account, including a refresh token when offline access was asked for.</summary>
/// <remarks>
/// A refresh token is issued on the same terms Auth0 issues one: the login has to ask for `offline_access`,
/// which the application's own login does. Rotating it on every exchange is this provider's own choice.
/// </remarks>
object TokenResponseFor(string authority, string clientId, Account account, string? nonce, string? scope)
{
    var grantedScope = string.IsNullOrEmpty(scope) ? "openid profile email" : scope;

    var response = new Dictionary<string, object>
    {
        ["access_token"] = CreateToken(authority, clientId, account, nonce: null, accessToken: true),
        ["id_token"] = CreateToken(authority, clientId, account, nonce, accessToken: false),
        ["token_type"] = "Bearer",
        ["expires_in"] = accessTokenLifetimeSeconds,
        ["scope"] = grantedScope,
    };

    if (grantedScope.Contains("offline_access", StringComparison.Ordinal))
    {
        var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

        refreshTokens[refreshToken] = account.Subject;
        response["refresh_token"] = refreshToken;
    }

    return response;
}

static string? Bearer(HttpRequest request)
{
    var header = request.Headers.Authorization.ToString();

    return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? header["Bearer ".Length..].Trim()
        : null;
}

static bool PkceMatches(string? challenge, string? method, string? verifier)
{
    if (string.IsNullOrEmpty(challenge))
    {
        return true;
    }

    if (string.IsNullOrEmpty(verifier))
    {
        return false;
    }

    // The application asks for S256; plain is accepted only so a misconfiguration is visible rather
    // than silently refused.
    return method is "plain"
        ? string.Equals(challenge, verifier, StringComparison.Ordinal)
        : string.Equals(
            challenge,
            Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
            StringComparison.Ordinal);
}

static string LoginPage(string? queryString, IReadOnlyDictionary<string, Account> accounts)
{
    var rows = new StringBuilder();

    foreach (var (key, account) in accounts)
    {
        rows.Append("<li><a href=\"/authorize")
            .Append(WebUtility.HtmlEncode(queryString))
            .Append("&amp;account=")
            .Append(Uri.EscapeDataString(key))
            .Append("\">")
            .Append(WebUtility.HtmlEncode(account.Name))
            .Append("</a></li>");
    }

    return $"""
        <!doctype html>
        <html lang="en">
        <head><meta charset="utf-8"><title>Sign in — local provider</title></head>
        <body>
        <h1>Sign in to Core Rental</h1>
        <p>The browser suite's local identity provider. Choose an account.</p>
        <ul>{rows}</ul>
        </body>
        </html>
        """;
}
