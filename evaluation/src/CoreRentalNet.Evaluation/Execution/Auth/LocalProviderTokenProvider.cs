using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoreRentalNet.Evaluation.Configuration;

using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;
namespace CoreRentalNet.Evaluation.Execution.Auth;

/// <summary>Acquires a local-provider token and refreshes it before it expires.</summary>
/// <remarks>
/// The refresh token stays in this object. It is never written to a report, trace, or log. The provider
/// is for the repository's local OIDC server; a deployed evaluation should use client credentials.
/// </remarks>
public sealed class LocalProviderTokenProvider(
    EvaluationOptions options,
    HttpClient httpClient,
    TimeProvider timeProvider) : ICallerTokenProvider
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CallerToken? _accessToken;
    private string? _refreshToken;

    /// <inheritdoc />
    public async Task<CallerToken> GetAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var now = timeProvider.GetUtcNow();

            if (_accessToken is { ExpiresAt: { } expiry }
                && expiry - now > options.TokenRefreshMargin)
            {
                return _accessToken;
            }

            return _refreshToken is not null
                ? await RefreshAsync(cancellationToken).ConfigureAwait(false)
                : await AuthorizeAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<CallerToken> AuthorizeAsync(CancellationToken cancellationToken)
    {
        var verifier = LocalProviderProtocol.Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = LocalProviderProtocol.Base64Url(
            SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = options.AuthClientId,
            ["redirect_uri"] = options.AuthRedirectUri,
            ["scope"] = "openid profile email offline_access",
            ["state"] = "evaluation",
            ["nonce"] = "evaluation",
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["account"] = options.AuthAccount,
        };

        var authorizeUri = new Uri(
            $"{options.AuthAuthority.TrimEnd('/')}/authorize?{LocalProviderProtocol.Form(query)}");

        using var response = await httpClient
            .GetAsync(authorizeUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!LocalProviderProtocol.IsRedirect(response.StatusCode)
            || response.Headers.Location is not { } location)
        {
            throw new CallerTokenException(
                $"The local provider did not redirect the authorization request ({(int)response.StatusCode}).");
        }

        var code = LocalProviderProtocol.QueryValue(new Uri(authorizeUri, location), "code")
            ?? throw new CallerTokenException("The local provider redirect carried no authorization code.");

        return await ExchangeAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["client_id"] = options.AuthClientId,
                ["redirect_uri"] = options.AuthRedirectUri,
                ["code_verifier"] = verifier,
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<CallerToken> RefreshAsync(CancellationToken cancellationToken)
    {
        return await ExchangeAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = _refreshToken!,
                ["client_id"] = options.AuthClientId,
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<CallerToken> ExchangeAsync(
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(form);
        using var response = await httpClient
            .PostAsync(new Uri($"{options.AuthAuthority.TrimEnd('/')}/oauth/token"), content, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new CallerTokenException(
                $"The local provider refused the token request ({(int)response.StatusCode}).");
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        var accessToken = root.TryGetProperty("access_token", out var access)
            && access.ValueKind == JsonValueKind.String
                ? access.GetString()
                : null;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new CallerTokenException("The local provider returned no access token.");
        }

        var token = CallerTokenParser.Parse(accessToken);

        if (root.TryGetProperty("expires_in", out var expires)
            && expires.TryGetInt32(out var seconds)
            && seconds > 0)
        {
            token = token with
            {
                ExpiresAt = timeProvider.GetUtcNow().AddSeconds(seconds),
            };
        }

        _accessToken = token;
        _refreshToken = root.TryGetProperty("refresh_token", out var refresh)
            && refresh.ValueKind == JsonValueKind.String
                ? refresh.GetString() ?? _refreshToken
                : _refreshToken;

        return token;
    }
}
