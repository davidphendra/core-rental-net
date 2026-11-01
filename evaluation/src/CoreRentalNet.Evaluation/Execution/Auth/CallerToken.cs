using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;

namespace CoreRentalNet.Evaluation.Execution.Auth;

/// <summary>The caller token and the claims the preflight needs, without the token being logged.</summary>
public sealed record CallerToken(
    string Value,
    DateTimeOffset? ExpiresAt,
    string? Issuer,
    IReadOnlyList<string> Audiences,
    IReadOnlyList<string> Permissions);
