namespace CoreRentalNet.Host.Infrastructure;

/// <summary>
/// What a requirement means in a deployment that has no identity provider to check against.
/// </summary>
/// <remarks>
/// A property of the permission rather than of the handler, because it is a decision about what the
/// permission guards. Reading the catalogue is served from memory and costs nothing to allow, so a
/// demonstration with no provider opens it. Generating a suggestion spends model calls and an external
/// round trip, so the same deployment must not open that - and a handler that could not tell the two
/// apart could only get one of them right.
/// </remarks>
internal enum ClaimBehavior
{
    /// <summary>Everyone is entitled when there is nobody to check.</summary>
    Open = 1,

    /// <summary>Nobody is entitled when there is nobody to check.</summary>
    Closed = 2,
}
