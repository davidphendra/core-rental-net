namespace CoreRentalNet.E2E.LocalProvider;

internal sealed record PendingCode(
    string ClientId,
    string RedirectUri,
    string Account,
    string? Nonce,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string Scope,
    DateTimeOffset ExpiresAt);
