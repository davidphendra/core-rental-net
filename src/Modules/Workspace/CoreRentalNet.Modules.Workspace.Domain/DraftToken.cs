using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>
/// The opaque handle that addresses a draft without an account. A distinct type from the order
/// access token so the two cannot be confused, but the same mechanism underneath.
/// </summary>
public sealed record DraftToken
{
    private readonly OpaqueToken token;

    private DraftToken(OpaqueToken token) => this.token = token;

    public string Hash => token.Hash;

    public static string IssueRawToken() => OpaqueToken.IssueRawToken();

    public static DraftToken FromRawToken(string rawToken) => new(OpaqueToken.FromRawToken(rawToken));

    public static DraftToken FromHash(string hash) => new(OpaqueToken.FromHash(hash));

    public static string HashOf(string rawToken) => OpaqueToken.HashOf(rawToken);

    public override string ToString() => Hash;
}
