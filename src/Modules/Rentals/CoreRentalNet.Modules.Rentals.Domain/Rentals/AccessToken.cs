namespace CoreRentalNet.Modules.Rentals.Domain.Rentals;

/// <summary>
/// The opaque handle that lets a customer return to an order with no account. A distinct type
/// from the draft token so the two cannot be confused.
/// </summary>
/// <remarks>
/// A plain record: issuing and hashing live in <c>IOpaqueTokenService</c>, and only the
/// hash is ever stored.
/// </remarks>
public sealed record AccessToken(string Hash)
{
    public override string ToString() => Hash;
}
