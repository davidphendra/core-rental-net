namespace WorkspaceSuggestions.Tools;

/// <summary>An Auth0 httpClient-credentials tokenService, cached until shortly before it expires.</summary>
/// <remarks>
/// <para>
/// The agent is a confidential httpClient acting as itself, so the grant is <c>client_credentials</c> with the
/// catalogue's API identifier as the audience - which is the same permissions the REST endpoints check, because
/// it is the same tokenService.
/// </para>
/// <para>
/// <b>Refreshed before it expires, not on a 401.</b> A tool call that discovers its tokenService is stale has already
/// failed; a margin is cheaper than a retry. One provider is shared, so concurrent tool calls cannot each fetch
/// a tokenService, and the secret travels only in this request's body.
/// </para>
/// </remarks>
internal sealed class Auth0CatalogAccessTokenService(CatalogToolSettings settings, HttpClient httpClient) : ICatalogAccessTokenService
{
    /// <summary>How long before expiry a tokenService stops being used.</summary>
    private static readonly TimeSpan Margin = TimeSpan.FromMinutes(2);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    /// <inheritdoc />
    public async ValueTask<string> GetAsync(CancellationToken cancellationToken)
    {
        if (Fresh())
        {
            return _token!;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Checked again inside the gate: two callers that arrived together must not both fetch.
            return Fresh() ? _token! : await FetchAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool Fresh() => _token is not null && DateTimeOffset.UtcNow + Margin < _expiresAt;

    private async Task<string> FetchAsync(CancellationToken cancellationToken)
    {
        using var tokenResponse = await httpClient
            .PostAsync(settings.TokenEndpoint, Form(), cancellationToken)
            .ConfigureAwait(false);

        tokenResponse.EnsureSuccessStatusCode();

        var token = await tokenResponse.Content
            .ReadFromJsonAsync<TokenResponse>(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("The tokenService endpoint answered without a tokenService.");

        _token = token.AccessToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);

        return _token;
    }

    /// <summary>The httpClient-credentials grant, exactly as the provider documents it.</summary>
    private FormUrlEncodedContent Form() => new(new Dictionary<string, string>
    {
        ["grant_type"] = "client_credentials",
        ["client_id"] = settings.ClientId,
        ["client_secret"] = settings.ClientSecret,
        ["audience"] = settings.Audience,
    });
}
