namespace CoreRentalNet.Host.Infrastructure;

/// <summary>The caller's own access token, produced only while it can still be relied on for a run.</summary>
/// <remarks>
/// <para>
/// <b>The port is here because the token is the caller's, not a module's.</b> A run is handed the token its
/// caller presented so the catalogue can answer for that caller, and reading it is a concern of the request.
/// That is why this lives beside <see cref="TokenHandler"/> rather than in a module.
/// </para>
/// <para>
/// <b>The implementation reads through the identity SDK, which is where expiry is decided.</b> This states the
/// one thing a caller needs - a token, or a failure it can turn into a re-login - so no caller reads an
/// <c>exp</c> claim and no caller owns a refresh.
/// </para>
/// </remarks>
internal interface IAccessTokenService
{
    /// <summary>The caller's token, for the run that is about to be started.</summary>
    /// <exception cref="CallerAccessTokenUnavailableException">
    /// The session cannot produce a token the identity SDK will vouch for.
    /// </exception>
    Task<string> GetAsync();
}
