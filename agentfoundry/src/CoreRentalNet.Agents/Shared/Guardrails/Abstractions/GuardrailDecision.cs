namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>Whether a call may proceed, and when it may not, why — the reason is what the model is told.</summary>
internal sealed record GuardrailDecision(bool Allowed, string? Reason = null)
{
    public static GuardrailDecision Allow() => new(true);

    public static GuardrailDecision Deny(string reason) => new(false, reason);
}
