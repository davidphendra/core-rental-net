namespace CoreRentalNet.Host.Infrastructure;

/// <summary>The session could not produce an access token the catalogue will accept.</summary>
/// <remarks>
/// <para>
/// <b>Typed so the one caller that pays for a run can answer it as a session that ended rather than as a
/// failure of the application's.</b> An access token that has lapsed and cannot be renewed is not a defect; it
/// is a customer who has to sign in again, and the run endpoint words it that way.
/// </para>
/// <para>
/// <b>No expiry is decided here and no time is compared.</b> The identity SDK validates the recorded expiry
/// and exchanges the session's refresh token when it has passed; this is only the answer it could not give.
/// </para>
/// </remarks>
internal sealed class CallerAccessTokenUnavailableException : Exception
{
    /// <summary>The failure on its own.</summary>
    public CallerAccessTokenUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>The failure, with what the identity SDK said underneath it.</summary>
    public CallerAccessTokenUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
