using System.Security.Cryptography;
using System.Text;
using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>
/// The opaque handle that addresses a draft without an account.
/// </summary>
/// <remarks>
/// Only the hash is ever stored, and only the hash is ever compared. The raw value exists
/// once, in the cookie, and is never persisted or logged (ADR-0007).
/// </remarks>
public sealed record DraftToken
{
    public const int RawTokenBytes = 32;

    private DraftToken(string hash) => Hash = hash;

    public string Hash { get; }

    /// <summary>Issues a new raw token. This is the only moment the raw value exists.</summary>
    public static string IssueRawToken()
        => Base64Url(RandomNumberGenerator.GetBytes(RawTokenBytes));

    public static DraftToken FromRawToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new DomainRuleViolationException("A draft token is required.");
        }

        return new DraftToken(HashOf(rawToken));
    }

    /// <summary>Rehydrates from a stored hash, for looking a draft up.</summary>
    public static DraftToken FromHash(string hash)
    {
        if (hash.Length != 64 || !hash.All(char.IsAsciiHexDigit))
        {
            throw new DomainRuleViolationException("'hash' is not a draft token hash.");
        }

        return new DraftToken(hash.ToUpperInvariant());
    }

    public static string HashOf(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    public override string ToString() => Hash;

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
