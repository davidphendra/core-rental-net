using System.Security.Cryptography;
using System.Text;
using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>
/// Issues and hashes opaque tokens, moved off the Domain's value object.
/// </summary>
/// <remarks>
/// Only the hash is ever stored or compared; the raw value exists once, in the cookie or the link
/// that carries it, and is never persisted or logged.
/// </remarks>
public sealed class OpaqueTokenService : IOpaqueTokenService
{
    /// <summary>The byte count <see cref="IssueRawToken"/> uses when the caller names none.</summary>
    public const int DefaultRawBytes = 32;

    /// <summary>At least 128 bits, so the raw value cannot be guessed.</summary>
    public const int MinimumRawBytes = 16;

    /// <inheritdoc />
    public string IssueRawToken(int bytes = DefaultRawBytes)
    {
        if (bytes < MinimumRawBytes)
        {
            throw new DomainRuleViolationException($"A token needs at least {MinimumRawBytes} bytes of entropy, but {bytes} were requested.");
        }

        return Base64Url(RandomNumberGenerator.GetBytes(bytes));
    }

    /// <inheritdoc />
    public string HashOf(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new DomainRuleViolationException("A token is required.");
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
