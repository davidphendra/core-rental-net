namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>The caller's own token, held for one call and given up when the call no longer needs it.</summary>
/// <remarks>
/// <para>
/// Replaces the client-credentials exchange: the credential is the caller's, so nothing is fetched and no secret
/// lives in the container. A call that reaches the catalogue without one fails loudly rather than presenting
/// the process as the caller.
/// </para>
/// <para>
/// <b>Two ways out, and they mean different things.</b> <see cref="Release"/> is the run saying it has finished
/// with the credential — a point in the pipeline that can be read and tested, which the terminal stage reaches
/// before it announces its ending. <see cref="IDisposable.Dispose"/> is the call saying it is over, and it is what
/// makes the holder's lifetime the call's rather than the process's: the scope's disposal is the backstop, so a
/// path that forgets to release still cannot keep a token.
/// </para>
/// </remarks>
internal interface IMcpAccessTokenService : IDisposable
{
    /// <summary>The token this call presents, or null until the call's agent has read it from the request.</summary>
    string? Token { get; set; }

    /// <summary>The token, or a failure when the request carried none.</summary>
    ValueTask<string> GetAsync(CancellationToken cancellationToken);

    /// <summary>Gives the token up, because nothing will present it again.</summary>
    void Release();
}
