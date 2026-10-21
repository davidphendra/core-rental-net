namespace CoreRentalNet.Evaluation.Execution;

/// <summary>The caller token and the claims the preflight needs, without the token being logged.</summary>
public sealed record CallerToken(
    string Value,
    DateTimeOffset? ExpiresAt,
    string? Issuer,
    IReadOnlyList<string> Audiences,
    IReadOnlyList<string> Permissions);
