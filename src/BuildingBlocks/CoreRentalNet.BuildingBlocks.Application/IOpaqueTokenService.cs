using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>
/// Issues and hashes the unguessable handles that address a draft or an order without an account.
/// Moved off the <c>OpaqueToken</c> value object.
/// </summary>
public interface IOpaqueTokenService
{
    /// <summary>Issues a new raw token. This is the only moment the raw value exists.</summary>
    /// <exception cref="DomainRuleViolationException">
    /// Fewer than <see cref="OpaqueTokenService.MinimumRawBytes"/> bytes were requested, so the raw
    /// value would be guessable.
    /// </exception>
    string IssueRawToken(int bytes = 32);

    /// <summary>The stored hash of a raw token. Refuses a blank raw token.</summary>
    /// <exception cref="DomainRuleViolationException">The raw token is null, empty or whitespace.</exception>
    string HashOf(string rawToken);
}
