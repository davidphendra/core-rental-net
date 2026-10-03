namespace CoreRentalNet.Agents.Shared.ChatClients;

/// <summary>A stage's answer repeated the caller's catalogue token, so the run must fail rather than continue.</summary>
/// <remarks>
/// A dedicated type so the one failure a tool caller must not convert into "the catalogue was unavailable" can be
/// told apart from a tool failure. It derives from <see cref="InvalidOperationException"/> because that is what the
/// guardrail has always thrown, so anything that already catches that type still does.
/// </remarks>
internal sealed class CallerTokenLeakException(string message) : InvalidOperationException(message);
