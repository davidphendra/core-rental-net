namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>
/// The opaque handle that addresses a draft without an account. A distinct type from the order
/// access token so the two cannot be confused.
/// </summary>
/// <remarks>
/// A plain record: issuing and hashing live in <c>IOpaqueTokenService</c>, and only the
/// hash is ever stored.
/// </remarks>
public sealed record DraftToken(string Hash)
{
    public override string ToString() => Hash;
}
