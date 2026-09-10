using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>
/// The opaque handle that lets a customer return to an order with no account. A distinct type
/// from the draft token so the two cannot be confused; the same mechanism underneath.
/// </summary>
public sealed record AccessToken
{
    private readonly OpaqueToken token;

    private AccessToken(OpaqueToken token) => this.token = token;

    public string Hash => token.Hash;

    public static string IssueRawToken() => OpaqueToken.IssueRawToken();

    public static AccessToken FromRawToken(string rawToken) => new(OpaqueToken.FromRawToken(rawToken));

    public static AccessToken FromHash(string hash) => new(OpaqueToken.FromHash(hash));

    public static string HashOf(string rawToken) => OpaqueToken.HashOf(rawToken);

    public override string ToString() => Hash;
}
