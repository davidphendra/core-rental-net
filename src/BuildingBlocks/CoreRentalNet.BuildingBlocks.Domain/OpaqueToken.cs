using System.Security.Cryptography;
using System.Text;

namespace CoreRentalNet.BuildingBlocks.Domain;

/// <summary>
/// An unguessable handle that addresses something without an account.
/// </summary>
/// <remarks>
/// Only the hash is ever stored or compared; the raw value exists once, in the cookie or the
/// link that carries it, and is never persisted or logged (ADR-0007). The draft token and the
/// order access token are both this, kept as distinct types so one cannot be passed where the
/// other is expected.
/// </remarks>
public sealed record OpaqueToken
{
    public const int DefaultRawBytes = 32;

    private OpaqueToken(string hash) => Hash = hash;

    public string Hash { get; }

    /// <summary>Issues a new raw token. This is the only moment the raw value exists.</summary>
    public static string IssueRawToken(int bytes = DefaultRawBytes)
    {
        if (bytes < 16)
        {
            throw new DomainRuleViolationException($"A token needs at least 16 bytes of entropy, but {bytes} were requested.");
        }

        return Base64Url(RandomNumberGenerator.GetBytes(bytes));
    }

    public static OpaqueToken FromRawToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new DomainRuleViolationException("A token is required.");
        }

        return new OpaqueToken(HashOf(rawToken));
    }

    /// <summary>Rehydrates from a stored hash, for looking something up.</summary>
    public static OpaqueToken FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash) || hash.Length != 64 || !hash.All(char.IsAsciiHexDigit))
        {
            throw new DomainRuleViolationException("That is not a token hash.");
        }

        return new OpaqueToken(hash.ToUpperInvariant());
    }

    public static string HashOf(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    public override string ToString() => Hash;

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
